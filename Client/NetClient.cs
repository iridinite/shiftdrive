/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    internal static class NetClient {
        private static Iridinite.Networking.Client socket;

        public static GameState World { get; private set; }
        public static bool IsConnecting { get; private set; }
        public static bool Connected => socket != null && socket.Connected;

        public static bool SimRunning { get; private set; }
        public static int PlayerCount { get; private set; }
        public static PlayerRole TakenRoles { get; private set; }
        public static PlayerRole MyRoles { get; private set; }

        public static List<CommMessage> Inbox { get; private set; }

        private static Action<bool, string> connectCallback;
        private static bool expectShutdown;

        internal static readonly object worldLock = new object();

        public static event Action<string> Announcement;
        public static event Action<CommMessage> CommsReceived;

        public static void Disconnect() {
            expectShutdown = true;
            if (socket != null && socket.Connected)
                socket.Disconnect();
            socket = null;
        }

        public static void Connect(string host, Action<bool, string> _callback) {
            // make sure any old sockets is disposed of
            Disconnect();

            // reset world
            World = new GameState();
            World.IsServer = false;

            // reset client state
            Inbox = new List<CommMessage>();
            connectCallback = _callback;
            expectShutdown = false;
            IsConnecting = true;
            TakenRoles = 0;
            MyRoles = 0;

            // initialize socket and bind events
            socket = new Iridinite.Networking.Client();
            socket.OnConnected += Client_OnConnected;
            socket.OnDisconnected += Client_OnDisconnected;
            socket.OnError += Client_OnError;
            socket.OnDataReceived += Client_OnDataReceived;
            // connect to remote host
            Logger.Log("Connecting...");
            socket.RemoteIP = host;
            socket.RemotePort = Config.ServerPort;
            socket.Connect();
        }

        public static void Send(Packet packet) {
            socket.Send(packet.ToArray());
        }

        private static void Client_OnConnected() {
            Debug.Assert(Connected);
            Logger.Log("Connected to server! Sending handshake.");

            // report back to the connect UI that we're connected
            connectCallback(true, null);
            IsConnecting = false;

            // send a handshake packet
            using (Packet packet = new Packet(PacketID.Handshake)) {
                packet.Write(NetShared.ProtocolVersion);
                Send(packet);
            }
        }

        private static void Client_OnDisconnected() {
            // show a 'connection lost' message if the disconnect was unexpected
            if (!expectShutdown)
                SDGame.Inst.SetUIRoot(new FormMessage(Locale.Get("err_connlost")));
            Logger.Log("Disconnected from server.");
        }

        private static void Client_OnError(Exception ex) {
            if (IsConnecting) {
                SocketException sockex = ex as SocketException;
                Debug.Assert(sockex != null);
                connectCallback(false, $"({sockex.SocketErrorCode}){Environment.NewLine}{sockex.Message}");
            } else {
                Logger.LogError("Client error: " + ex);
#if DEBUG
                Debugger.Break();
#endif
                throw ex;
            }
        }

        private static void Client_OnDataReceived(byte[] bytes) {
            using (Packet recv = new Packet(bytes)) {
                try {
                    switch (recv.GetID()) {
                        case PacketID.Handshake:
                            // require same protocol versions
                            if (recv.GetLength() != 1 || recv.ReadByte() != NetShared.ProtocolVersion)
                                throw new Exception(Locale.Get("err_version"));
                            break;

                        case PacketID.LobbyState:
                            SimRunning = recv.ReadBoolean();
                            PlayerCount = recv.ReadUInt16();
                            TakenRoles = (PlayerRole)recv.ReadByte();
                            break;

                        case PacketID.GameUpdate:
                            lock (worldLock) {
                                // read game state from packet
                                World.Deserialize(recv);
                            }
                            break;

                        case PacketID.SelectRole:
                            // server replies with the list of roles this client currently has
                            MyRoles = (PlayerRole)recv.ReadByte();
                            break;

                        case PacketID.EnterGame:
                            SDGame.Inst.SetUIRoot(new FormGame());
                            break;

                        case PacketID.Announcement:
                            string announce = null;
                            AnnouncementId id = (AnnouncementId)recv.ReadByte();
                            switch (id) {
                                case AnnouncementId.Custom: announce = recv.ReadString(); break;
                                case AnnouncementId.FuelLow: announce = Locale.Get("announce_fuellow"); break;
                                case AnnouncementId.FuelCritical: announce = Locale.Get("announce_fuelcrit"); break;
                                case AnnouncementId.Hull75: announce = Locale.Get("announce_hull75"); break;
                                case AnnouncementId.Hull50: announce = Locale.Get("announce_hull50"); break;
                                case AnnouncementId.Hull25: announce = Locale.Get("announce_hull25"); break;
                                case AnnouncementId.BlackHole: announce = Locale.Get("announce_blackhole"); break;
                                case AnnouncementId.ShieldLow: announce = Locale.Get("announce_shieldlow"); break;
                                case AnnouncementId.ShieldDown: announce = Locale.Get("announce_shielddown"); break;
                                case AnnouncementId.ShieldUp: announce = Locale.Get("announce_shieldup"); break;
                                case AnnouncementId.ShiftInitialize: announce = Locale.Get("announce_shiftinit"); break;
                                case AnnouncementId.ShiftCharged: announce = Locale.Get("announce_shiftsoon"); break;
                                default:
                                    throw new InvalidDataException(Locale.Get("err_unknownannounce"));
                            }
                            Announcement?.Invoke(announce);
                            break;

                        case PacketID.ParticleEffect:
                            ParticleEffect effect = (ParticleEffect)recv.ReadByte();
                            Vector2 position = new Vector2(recv.ReadSingle(), recv.ReadSingle());
                            switch (effect) {
                                case ParticleEffect.Explosion:
                                    ParticleManager.CreateExplosion(position);
                                    break;
                                case ParticleEffect.BulletImpact:
                                    ParticleManager.CreateBulletImpact(position, 0f);
                                    break;
                            }
                            break;

                        case PacketID.IntelGetText:
                            CommMessage msg = new CommMessage(recv.ReadString(), recv.ReadString());
                            lock (Inbox) {
                                // insert the message at the front
                                Inbox.Insert(0, msg);
                            }
                            CommsReceived?.Invoke(msg);
                            break;

                        default:
                            Logger.LogError("Client got unknown packet " + recv.GetID());
                            throw new InvalidDataException(Locale.Get("err_unknownpacket") + " (" + recv.GetID() + ")");
                    }
                } catch (Exception ex) {
                    Disconnect();
                    Logger.LogError("Client error in OnDataReceived: " + ex);
                    SDGame.Inst.SetUIRoot(new FormMessage(Locale.Get("err_comm") + Environment.NewLine + ex.ToString()));
                }
            }
        }

    }

}
