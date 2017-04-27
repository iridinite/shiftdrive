/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.Xna.Framework;
using Iridinite.Networking;

namespace ShiftDrive {

    /// <summary>
    /// Manages a game server and associated game state.
    /// </summary>
    internal static class NetServer {

        /// <summary>
        /// Represents a single connected player with associated state.
        /// </summary>
        private sealed class NetPlayer {
            public bool authorized;
            public bool ready;
            public PlayerRole roles;
        }

        /// <summary>
        /// Contains information about the progress of a damaging explosion.
        /// </summary>
        private sealed class ExplosionData {
            public Vector2 position;
            public float range;
            public float damage;
            public float life;
        }

        internal static GameState world { get; private set; }
        internal static bool Active { get { return socket != null && socket.Listening; } }

        public const float MAPSIZE = 1000f;

        private static Host socket;
        private static LuaState lua;

        private static Dictionary<int, NetPlayer> players;
        private static Dictionary<AnnouncementId, float> announceCooldown;
        private static List<ExplosionData> explosions;

        private static float heartbeatMax;
        private static float heartbeatTimer;
        private static bool simRunning;

        public static bool PrepareWorld() {
            GameObject.ResetIds();
            simRunning = false;
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

            players = new Dictionary<int, NetPlayer>();
            announceCooldown = new Dictionary<AnnouncementId, float>();
            explosions = new List<ExplosionData>();

            SDGame.Logger.Log("Starting server...");
            socket = new Host();
            socket.OnClientConnect += Socket_OnClientConnect;
            socket.OnClientDisconnect += Socket_OnClientDisconnect;
            socket.OnServerStart += Socket_OnServerStart;
            socket.OnServerStop += Socket_OnServerStop;
            socket.OnDataReceived += Socket_OnDataReceived;
            socket.OnError += Socket_OnError;
            socket.LocalIP = IPAddress.Any;
            socket.MaxClients = 100;
            socket.Start();
        }

        public static void Update(GameTime gameTime) {
            // can only update if hosting
            if (socket == null || !socket.Listening) return;

            // don't update if still in lobby
            if (!simRunning) return;

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

            // update explosion advances
            for (int i = explosions.Count - 1; i >= 0; i--) {
                ExplosionData expl = explosions[i];
                foreach (uint key in keys) {
                    // iterate over all objects (QueryGrid is too unreliable)
                    GameObject gobj = world.Objects[key];
                    // object within range?
                    float dist = Vector2.DistanceSquared(gobj.position, expl.position);
                    if (dist > expl.range * expl.life)
                        continue;
                    // deal damage based on distance and time
                    //dist = (float)Math.Sqrt(dist);
                    gobj.TakeDamage(expl.damage * (1f - expl.life) * dt, true);
                }
                // remove explosions that are >1 sec old
                expl.life += dt;
                if (expl.life >= 1f) explosions.RemoveAt(i);
            }

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
                packet.Write(simRunning);
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
                world.Serialize(packet, false);
                socket.Broadcast(packet.ToArray());
            }
        }

        private static void BeginGame() {
            simRunning = true;
            BroadcastLobbyState();

            using (Packet reply = new Packet(PacketID.EnterGame))
                socket.Broadcast(reply.ToArray());
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
        /// Simulates an explosion by damaging nearby objects and publishing a particle effect.
        /// </summary>
        /// <param name="position">The world position of the explosion.</param>
        /// <param name="range">The maximum damaging range of the explosion in world units.</param>
        /// <param name="damage">The maximum damage applied to objects per second of exposure.</param>
        public static void DoDamagingExplosion(Vector2 position, float range, float damage) {
            ExplosionData expl = new ExplosionData();
            expl.position = position;
            expl.damage = damage;
            expl.range = range * range; // DistanceSquared
            expl.life = 0f;
            explosions.Add(expl);

            PublishParticleEffect(ParticleEffect.Explosion, position);
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

                    // shorthand reference for the player ship
                    PlayerShip pship = world.GetPlayerShip();

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
                            // send this player a copy of the game world
                            using (Packet worldpacket = new Packet(PacketID.GameUpdate)) {
                                world.Serialize(worldpacket, true); // client knows nothing about this map yet
                                socket.Send(clientID, worldpacket.ToArray());
                            }
                            break;

                        case PacketID.SelectRole:
                            if (recv.GetLength() != 1) {
                                socket.Kick(clientID);
                                return;
                            }
                            // save role choice and confirm back to client
                            player.roles ^= (PlayerRole)recv.ReadByte();
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

                            if (simRunning) {
                                // if game already started, invite this player to the game
                                using (Packet reply = new Packet(PacketID.EnterGame))
                                    socket.Broadcast(reply.ToArray());
                            } else {
                                // start game when all players are ready
                                // TODO
                                BeginGame();
                            }
                            break;

                        case PacketID.HelmSteering:
                            // Helm sets ship steering vector.
                            pship.steering = MathHelper.Clamp(recv.ReadSingle(), 0f, 360f);
                            pship.changed |= ObjectProperty.Steering;
                            break;

                        case PacketID.HelmThrottle:
                            // Helm sets ship throttle. Clamp to ensure input sanity.
                            pship.throttle = MathHelper.Clamp(recv.ReadSingle(), 0f, 1f);
                            pship.changed |= ObjectProperty.Throttle;
                            break;

                        case PacketID.WeapShields:
                            // Weapons toggles shield status
                            pship.shieldActive = !world.GetPlayerShip().shieldActive;
                            pship.changed |= ObjectProperty.Health;
                            break;

                        case PacketID.WeapTarget:
                            // Weapons selects or deselects a target
                            uint targetid = recv.ReadUInt32();
                            bool targeting = recv.ReadBoolean();
                            if (targeting)
                                pship.targets.Add(targetid);
                            else
                                pship.targets.Remove(targetid);
                            pship.changed |= ObjectProperty.Targets;
                            break;

                        default:
                            SDGame.Logger.LogError($"Server got unknown packet ({recv.GetID()}), client #{clientID}");
                            break;
                    }

                }

            } catch (Exception ex) {
                SDGame.Logger.LogError($"Server exception: {ex}");
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