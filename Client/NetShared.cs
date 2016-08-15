/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;
using System.IO.Compression;

namespace ShiftDrive {

    /// <summary>
    /// Describes the function of a packet.
    /// </summary>
    internal enum PacketType {
        Handshake = 1,
        LobbyState = 2,
        SelectRole = 3,
        Ready = 4,
        EnterGame = 5,
        GameUpdate = 6,
        Announcement = 7,

        HelmSteering = 8,
        HelmThrottle = 9,
        HelmShift = 10,

        WeapShields = 11,
        WeapTarget = 12,
        WeapMissileLoad = 13,
        WeapMissileLaunch = 14,

        EngiSetPower = 15,
        EngiDamageReport = 16,

        QuarOrder = 17,

        IntelScan = 18,
        IntelInText = 19,
        IntelSend = 20,
        IntelShopInfo = 21,
        IntelShopBuy = 22
    }

    /// <summary>
    /// Identifies a particular kind of announcement.
    /// </summary>
    internal enum AnnouncementId {
        Custom,
        FuelLow,
        FuelCritical,
        BlackHole,
        ShieldLow,
        ShieldDown,
        ShieldUp,
        ShiftInitialize,
        ShiftCharged,
        Hull75,
        Hull50,
        Hull25
    }

    /// <summary>
    /// Represents a networking packet.
    /// </summary>
    internal sealed class Packet {
        public PacketType Type { get; }
        public byte[] Bytes { get; }
        public byte[] Payload { get; }

        // constructs a packet based on the full packet array (received from network)
        public Packet(byte[] packet) {
            if (packet == null || packet.Length < 1)
                throw new ArgumentException();
            // copy out the packet ID and payload
            Bytes = packet;
            Type = (PacketType)packet[0];
            Payload = new byte[packet.Length - 1];
            Buffer.BlockCopy(packet, 1, Payload, 0, packet.Length - 1);
        }

        public Packet(PacketType type) {
            Type = type;
            Payload = new byte[0];
            Bytes = new byte[1] { (byte)type };
        }

        public Packet(PacketType type, byte[] payload) {
            if (payload == null) throw new ArgumentNullException();

            Type = type;
            Payload = payload;
            Bytes = new byte[payload.Length + 1];
            Bytes[0] = (byte)type;
            Buffer.BlockCopy(payload, 0, Bytes, 1, payload.Length);
        }

        public Packet(PacketType type, bool value) 
            : this(type, BitConverter.GetBytes(value)) {}

        public Packet(PacketType type, byte value)
            : this(type, new byte[1] { value }) {}
    }

    /// <summary>
    /// Helper functions for networking.
    /// </summary>
    internal static class NetShared {
        public const byte ProtocolVersion = 1;
        
        /// <summary>
        /// Compresses the specified byte array using a DeflateStream.
        /// </summary>
        public static byte[] CompressBuffer(byte[] b) {
            using (MemoryStream ms = new MemoryStream()) {
                using (DeflateStream zs = new DeflateStream(ms, CompressionMode.Compress, true)) {
                    zs.Write(b, 0, b.Length);
                }
                ms.Position = 0;

                byte[] compressed = new byte[(int)ms.Length];
                ms.Read(compressed, 0, compressed.Length);

                byte[] zipbuffer = new byte[compressed.Length + 4];
                Buffer.BlockCopy(compressed, 0, zipbuffer, 4, compressed.Length);
                Buffer.BlockCopy(BitConverter.GetBytes(b.Length), 0, zipbuffer, 0, 4);

                return zipbuffer;
            }
        }

        /// <summary>
        /// Decompresses a byte array compressed with CompressBuffer.
        /// </summary>
        public static byte[] DecompressBuffer(byte[] b) {
            using (MemoryStream ms = new MemoryStream()) {
                int datalen = BitConverter.ToInt32(b, 0);
                ms.Write(b, 4, b.Length - 4);

                byte[] decompressed = new byte[datalen];
                ms.Position = 0;
                using (DeflateStream zs = new DeflateStream(ms, CompressionMode.Decompress)) {
                    zs.Read(decompressed, 0, decompressed.Length);
                }

                return decompressed;
            }
        }

    }

}