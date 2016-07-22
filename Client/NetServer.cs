﻿/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Iridinite.Networking;

namespace ShiftDrive {
    
    internal sealed class NetPlayer {
        public bool authorized;
        public bool ready;
        public PlayerRole roles;
    }

    internal static class NetServer {
        internal static GameState world { get; private set; }
        
        private static Host socket;
        private static LuaState lua;

        private static Dictionary<int, NetPlayer> players;

        private static float heartbeatMax;
        private static float heartbeatTimer;
        
        public static void PrepareWorld() {
            world = new GameState();
            world.IsServer = true;

            SDGame.Logger.Log("Initializing Lua state...");
            lua = new LuaState();
            lua.Precompile();
            SDGame.Logger.Log("Precompilation finished.");
            lua.LoadFile("main");
            lua.Call(0, 0);
            lua.LoadFile("scenarios/siege");
            lua.Call(0, 0);
            SDGame.Logger.Log("Scripts ran successfully.");
        }

        public static void Start() {
            heartbeatMax = 0.06f;
            heartbeatTimer = 0f;

            if (socket != null && socket.Listening)
                Stop();

            SDGame.Logger.Log("Starting server...");
            socket = new Host();
            socket.OnClientConnect += Socket_OnClientConnect;
            socket.OnClientDisconnect += Socket_OnClientDisconnect;
            socket.OnServerStart += Socket_OnServerStart;
            socket.OnServerStop += Socket_OnServerStop;
            socket.OnDataReceived += Socket_OnDataReceived;
            socket.OnError += Socket_OnError;
            socket.MaxClients = 100;
            socket.Start();
        }

        public static void Update(GameTime gameTime) {
            // can only update if hosting
            if (socket == null || !socket.Listening) return;

            // update all gameobjects. use a backwards loop because some
            // objects may be scheduled for deletion and thus change the list order
            IEnumerable<ushort> keys = world.Objects.Keys.OrderByDescending(k => k);
            foreach (ushort key in keys) {
                GameObject gobj = world.Objects[key];
                gobj.Update(world, (float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            // decrement countdown timer
            heartbeatTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (heartbeatTimer > 0f) return;

            heartbeatTimer = heartbeatMax;
            BroadcastGameState();

            // destroy objects that should be deleted, now that they have also been
            // broadcast to clients as being deleted
            foreach (ushort key in keys) {
                GameObject gobj = world.Objects[key];
                if (gobj.ShouldDestroy()) world.Objects.Remove(key);
            }
        }
        
        public static void Stop() {
            SDGame.Logger.Log("Stopping server and closing Lua state.");
            lua.Destroy();
            socket.Stop();
        }

        public static void AddObject(GameObject obj) {
            world.Objects.Add(obj.id, obj);
        }

        private static void Socket_OnServerStart() {
            SDGame.Logger.Log("Server socket started.");
            players = new Dictionary<int, NetPlayer>();
        }

        private static void Socket_OnServerStop() {
            SDGame.Logger.Log("Server stopped.");
            players.Clear();
        }

        private static void Socket_OnClientConnect(int clientID) {
            // add an entry for this new client into the player table
            NetPlayer newplr = new NetPlayer();
            newplr.roles = 0;
            newplr.ready = false;
            newplr.authorized = false;
            players.Add(clientID, newplr);
            SDGame.Logger.Log($"Client connected (#{clientID} @ {socket.GetClientIP(clientID)}).");
        }

        private static void Socket_OnClientDisconnect(int clientID) {
            // remove this client from the player table
            players.Remove(clientID);
            BroadcastLobbyState();
            SDGame.Logger.Log($"Client #{clientID} disconnected.");
        }

        private static void BroadcastLobbyState() {
            // create a packet describing the lobby state
            Packet p;
            using (MemoryStream ms = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(ms)) {
                    writer.Write(false);
                    writer.Write((ushort)players.Count);
                    // sum up all taken role bitmasks
                    PlayerRole taken = 0;
                    foreach (NetPlayer otherplayer in players.Values)
                        taken |= otherplayer.roles;
                    writer.Write((byte)taken);
                }
                p = new Packet(PacketType.LobbyState, ms.ToArray());
            }
            // send the packet to all clients
            socket.Broadcast(p.Bytes);
        }

        private static void BroadcastGameState() {
            // build a byte array containing the world state
            byte[] worldbytes;
            using (MemoryStream ms = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(ms)) {
                    world.Serialize(writer);
                }
                worldbytes = ms.ToArray();
            }

            // compress the world state and create a packet
            byte[] compressed = NetShared.CompressBuffer(worldbytes);

            // send the compressed state out to all connected clients
            Packet gspacket = new Packet(PacketType.GameUpdate, compressed);
            socket.Broadcast(gspacket.Bytes);
        }

        private static void Socket_OnDataReceived(int clientID, byte[] packetbytes) {
            try {
                // find the client associated with the client id
                NetPlayer player = players[clientID];
                // reconstruct packet from received bytes
                Packet packet = new Packet(packetbytes);

                // if the packet is anything but a handshake and this user has not yet handshaked, kick the client.
                // might later replace this with some kind of actual authentication if necessary. Lua checksums maybe?
                if (packet.Type != PacketType.Handshake && !player.authorized) {
                    socket.Kick(clientID);
                    return;
                }

                // handle the packet
                switch (packet.Type) {
                    case PacketType.Handshake:
                        // require same protocol versions
                        if (packet.Payload.Length != 1 || packet.Payload[0] != NetShared.ProtocolVersion) {
                            socket.Kick(clientID);
                            return;
                        }
                        player.authorized = true;
                        // send a handshake response and then broadcast lobby
                        Packet handshake = new Packet(PacketType.Handshake, NetShared.ProtocolVersion);
                        socket.Send(clientID, handshake.Bytes);
                        BroadcastLobbyState();
                        break;

                    case PacketType.SelectRole:
                        if (packet.Payload.Length != 1) {
                            socket.Kick(clientID);
                            return;
                        }
                        player.roles ^= (PlayerRole)packet.Payload[0];
                        Packet confirmroles = new Packet(PacketType.SelectRole, (byte)player.roles);
                        socket.Send(clientID, confirmroles.Bytes);
                        BroadcastLobbyState();
                        SDGame.Logger.Log($"Client #{clientID} has roles {player.roles}.");
                        break;

                    case PacketType.Ready:
                        if (packet.Payload.Length != 1) {
                            socket.Kick(clientID);
                            return;
                        }
                        player.ready = (packet.Payload[0] == 1);
                        SDGame.Logger.Log($"Client #{clientID} ready: {player.ready}");
                        // temp begingame packet
                        Packet begingamePacket = new Packet(PacketType.EnterGame);
                        socket.Send(clientID, begingamePacket.Bytes);
                        break;

                    case PacketType.HelmSteering:
                        // Helm sets ship steering vector.
                        world.GetPlayerShip().steering = BitConverter.ToSingle(packet.Payload, 0);
                        world.GetPlayerShip().changed = true;
                        break;

                    case PacketType.HelmThrottle:
                        // Helm sets ship throttle. Clamp to ensure input sanity.
                        world.GetPlayerShip().throttle = MathHelper.Clamp(BitConverter.ToSingle(packet.Payload, 0), 0f, 1f);
                        world.GetPlayerShip().changed = true;
                        break;
                }

            } catch (Exception) {
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
                socket.Kick(clientID);
            }
        }

        private static void Socket_OnError(Exception ex) {
            SDGame.Logger.LogError(ex.ToString());
#if DEBUG
            System.Diagnostics.Debugger.Break();
#endif
        }

    }

}