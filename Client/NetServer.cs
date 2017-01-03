/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Iridinite.Networking;

namespace ShiftDrive {

    /// <summary>
    /// Represents a single connected player with associated state.
    /// </summary>
    internal sealed class NetPlayer {
        public bool authorized;
        public bool ready;
        public PlayerRole roles;
    }

    /// <summary>
    /// Manages a game server and associated game state.
    /// </summary>
    internal static class NetServer {
        internal static GameState world { get; private set; }
        internal static bool Active { get { return socket != null && socket.Listening; } }

        public const float MAPSIZE = 1000f;

        private static Host socket;
        private static LuaState lua;

        private static Dictionary<int, NetPlayer> players;
        private static Dictionary<AnnouncementId, float> announceCooldown;

        private static float heartbeatMax;
        private static float heartbeatTimer;

        public static bool PrepareWorld() {
            GameObject.ResetIds();
            world = new GameState();
            world.IsServer = true;

            try {
                SDGame.Logger.Log("Initializing Lua state...");
                lua = new LuaState();
                lua.Precompile();
                SDGame.Logger.Log("Precompilation finished.");
                lua.LoadFile("main");
                lua.Call(0, 0);
                lua.LoadFile("scenarios/siege");
                lua.Call(0, 0);
                SDGame.Logger.Log("Scripts ran successfully.");
                return true;

            } catch (LuaException e) {
                SDGame.Logger.LogError("Script execution failed.");
                SDGame.Logger.LogError(e.ToString());
                SDGame.Inst.ActiveForm = new FormMessage(e.ToString());
                return false;
            }
        }

        public static void Start() {
            Debug.Assert(!Active);

            heartbeatMax = 0.1f;
            heartbeatTimer = 0f;
            announceCooldown = new Dictionary<AnnouncementId, float>();

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
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            IEnumerable<uint> keys = world.Objects.Keys.OrderByDescending(k => k);
            foreach (uint key in keys) {
                GameObject gobj = world.Objects[key];
                gobj.Update(dt);
            }

            // update collision grid
            world.UpdateGrid();

            // update announcement cooldowns
            var announceKeys = announceCooldown.Keys.OrderBy(a => a);
            foreach (var key in announceKeys) {
                if (announceCooldown[key] > 0f) announceCooldown[key] -= dt;
            }

            // run Lua events
            lua.RunEvents();

            // decrement countdown timer
            heartbeatTimer -= dt;
            if (heartbeatTimer > 0f) return;

            heartbeatTimer = heartbeatMax;
            BroadcastGameState();

            // destroy objects that should be deleted, now that they have also been
            // broadcast to clients as being deleted
            foreach (uint key in keys) {
                GameObject gobj = world.Objects[key];
                if (gobj.IsDestroyScheduled()) world.Objects.Remove(key);
            }
        }

        public static void Stop() {
            SDGame.Logger.Log("Stopping server and closing Lua state.");
            lua.Dispose();
            lua = null;
            socket.Stop();
        }

        public static bool IsListening() {
            return socket != null && socket.Listening;
        }

        public static int GetPlayerCount() {
            return players.Count;
        }

#if DEBUG
        public static float GetHeartbeatTime() {
            return heartbeatMax;
        }

        public static int GetEventCount() {
            return lua.GetEventCount();
        }

        public static int GetLuaTop() {
            return LuaAPI.lua_gettop(lua.L);
        }

        public static float GetLuaMemory() {
            LuaAPI.lua_getglobal(lua.L, "collectgarbage");
            LuaAPI.lua_pushstring(lua.L, "count");
            LuaAPI.lua_call(lua.L, 1, 1);

            float result = (float)LuaAPI.lua_tonumber(lua.L, -1);
            LuaAPI.lua_pop(lua.L, 1);
            return result;
        }
#endif

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
            newplr.roles = (PlayerRole)(-1);
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
            using (Packet packet = new Packet(PacketID.LobbyState)) {
                packet.Write(false);
                packet.Write((ushort)players.Count);
                // sum up all taken role bitmasks
                PlayerRole taken = 0;
                foreach (NetPlayer otherplayer in players.Values)
                    taken |= otherplayer.roles;
                packet.Write((byte)taken);

                // send the packet to all clients
                socket.Broadcast(packet.ToArray());
            }
        }

        private static void BroadcastGameState() {
            // send the compressed state out to all connected clients
            using (Packet packet = new Packet(PacketID.GameUpdate)) {
                world.Serialize(packet);
                socket.Broadcast(packet.ToArray());
            }
        }

        public static void PublishAnnouncement(AnnouncementId annId, string customText = null) {
            // with the exception of custom announcement, don't spam them
            if (annId != AnnouncementId.Custom && announceCooldown.ContainsKey(annId) && announceCooldown[annId] > 0f) return;
            announceCooldown[annId] = 30f;

            using (Packet packet = new Packet(PacketID.Announcement)) {
                packet.Write((byte)annId);

                // custom announcement -> include custom text in packet
                if (annId == AnnouncementId.Custom) {
                    if (String.IsNullOrEmpty(customText))
                        throw new ArgumentNullException(nameof(customText));

                    packet.Write(customText);
                }

                socket.Broadcast(packet.ToArray());
            }
        }

        /// <summary>
        /// Instructs game clients to display the specified particle effect.
        /// </summary>
        /// <param name="effect">The effect to show.</param>
        /// <param name="position">The world position at which to place the effect.</param>
        public static void PublishParticleEffect(ParticleEffect effect, Vector2 position) {
            using (Packet packet = new Packet(PacketID.ParticleEffect)) {
                packet.Write((byte)effect);
                packet.Write(position.X);
                packet.Write(position.Y);
                socket.Broadcast(packet.ToArray());
            }
        }

        private static void Socket_OnDataReceived(int clientID, byte[] packetbytes) {
            try {
                using (Packet recv = new Packet(packetbytes)) {
                    // find the client associated with the client id
                    NetPlayer player = players[clientID];

                    // if the packet is anything but a handshake and this user has not yet handshaked, kick the client.
                    // might later replace this with some kind of actual authentication if necessary. Lua checksums maybe?
                    if (recv.GetID() != PacketID.Handshake && !player.authorized) {
                        socket.Kick(clientID);
                        return;
                    }

                    // handle the packet
                    switch (recv.GetID()) {
                        case PacketID.Handshake:
                            // require same protocol versions
                            if (recv.GetLength() != 1 || recv.ReadByte() != NetShared.ProtocolVersion) {
                                socket.Kick(clientID);
                                return;
                            }
                            // handshake OK, this connection can send game commands now
                            player.authorized = true;
                            // send a handshake response
                            using (Packet reply = new Packet(PacketID.Handshake)) {
                                reply.Write(NetShared.ProtocolVersion);
                                socket.Send(clientID, reply.ToArray());
                            }
                            // inform players of new connection
                            BroadcastLobbyState();
                            break;

                        case PacketID.SelectRole:
                            if (recv.GetLength() != 1) {
                                socket.Kick(clientID);
                                return;
                            }
                            // save role choice and confirm back to client
                            player.roles ^= (PlayerRole)recv.ReadByte();
                            Packet test = new Packet(PacketID.EnterGame);
                            using (Packet reply = new Packet(PacketID.SelectRole)) {
                                reply.Write((byte)player.roles);
                                socket.Send(clientID, reply.ToArray());
                            }
                            // update list of taken roles
                            BroadcastLobbyState();
                            SDGame.Logger.Log($"Client #{clientID} has roles {player.roles}.");
                            break;

                        case PacketID.Ready:
                            if (recv.GetLength() != 1) {
                                socket.Kick(clientID);
                                return;
                            }
                            player.ready = recv.ReadBoolean();
                            SDGame.Logger.Log($"Client #{clientID} ready: {player.ready}");
                            // TODO: wait until all players are ready
                            using (Packet reply = new Packet(PacketID.EnterGame))
                                socket.Send(clientID, reply.ToArray());
                            break;

                        case PacketID.HelmSteering:
                            // Helm sets ship steering vector.
                            world.GetPlayerShip().steering = MathHelper.Clamp(recv.ReadSingle(), 0f, 360f);
                            world.GetPlayerShip().changed |= ObjectProperty.Steering;
                            break;

                        case PacketID.HelmThrottle:
                            // Helm sets ship throttle. Clamp to ensure input sanity.
                            world.GetPlayerShip().throttle = MathHelper.Clamp(recv.ReadSingle(), 0f, 1f);
                            world.GetPlayerShip().changed |= ObjectProperty.Throttle;
                            break;
                    }

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