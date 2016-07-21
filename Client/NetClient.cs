/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;
using System.Diagnostics;

namespace ShiftDrive {

    internal static class NetClient {
        private static Iridinite.Networking.Client socket;

        public static GameState World { get; private set; }
        public static bool IsConnecting { get; private set; }
        public static bool Connected { get { return socket != null && socket.Connected; } }
        
        public static bool SimRunning { get; private set; }
        public static int PlayerCount { get; private set; }
        public static PlayerRole TakenRoles { get; private set; }
        public static PlayerRole MyRoles { get; private set; }

        private static Action<bool, string> connectCallback;
        private static bool expectShutdown;

        internal static readonly object worldLock = new object();

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
            SDGame.Logger.Log("Connecting...");
            socket.RemoteIP = host;
            socket.RemotePort = 8080;
            socket.Connect();
        }

        public static void Send(Packet packet) {
            socket.Send(packet.Bytes);
        }

        private static void Client_OnConnected() {
            Debug.Assert(Connected);
            SDGame.Logger.Log("Connected to server! Sending handshake.");

            // report back to the connect UI that we're connected
            connectCallback(true, null);
            IsConnecting = false;

            // send a handshake packet
            Packet handshake = new Packet(PacketType.Handshake, NetShared.ProtocolVersion);
            socket.Send(handshake.Bytes);
        }

        private static void Client_OnDisconnected() {
            // show a 'connection lost' message if the disconnect was unexpected
            if (!expectShutdown)
                SDGame.Inst.ActiveForm = new FormMessage(Utils.LocaleGet("err_connlost"));
            SDGame.Logger.Log("Disconnected from server.");
        }

        private static void Client_OnError(Exception ex) {
            if (IsConnecting) {
                SDGame.Logger.LogError("Failed to connect: " + ex.Message);
                connectCallback(false, ex.Message);
            } else {
                SDGame.Logger.LogError("Client error: " + ex);
#if DEBUG
                Debugger.Break();
#endif
                throw ex;
            }
        }

        private static void Client_OnDataReceived(byte[] bytes) {
            try {
                Packet packet = new Packet(bytes);
                switch (packet.Type) {
                    case PacketType.Handshake:
                        // require same protocol versions
                        if (packet.Payload.Length != 1 || packet.Payload[0] != NetShared.ProtocolVersion) {
                            throw new Exception(Utils.LocaleGet("err_version"));
                        }
                        break;

                    case PacketType.LobbyState:
                        using (MemoryStream ms = new MemoryStream(packet.Payload)) {
                            using (BinaryReader reader = new BinaryReader(ms)) {
                                SimRunning = reader.ReadBoolean();
                                PlayerCount = reader.ReadUInt16();
                                TakenRoles = (PlayerRole) reader.ReadByte();
                            }
                        }
                        break;

                    case PacketType.GameUpdate:
                        lock (worldLock) {
                            // first decompress the buffer received from the network
                            byte[] worldbytes = NetShared.DecompressBuffer(packet.Payload);
                            // then reconstruct the game state
                            using (MemoryStream ms = new MemoryStream(worldbytes)) {
                                using (BinaryReader reader = new BinaryReader(ms)) {
                                    World.Deserialize(reader);
                                }
                            }
                        }
                        break;

                    case PacketType.SelectRole:
                        // server replies with the list of roles this client currently has
                        MyRoles = (PlayerRole) packet.Payload[0];
                        break;

                    case PacketType.EnterGame:
                        SDGame.Inst.ActiveForm = new FormGame();
                        break;

                    default:
                        SDGame.Logger.LogError("Client got unknown packet " + packet.Type);
                        throw new InvalidDataException(Utils.LocaleGet("err_unknownpacket") + " (" + packet.Type + ")");
                }
            } catch (Exception ex) {
                Disconnect();
                SDGame.Logger.LogError("Client error in OnDataReceived: " + ex);
                SDGame.Inst.ActiveForm = new FormMessage(Utils.LocaleGet("err_comm") + Environment.NewLine + ex.ToString());
            }
        }
        
    }

}
