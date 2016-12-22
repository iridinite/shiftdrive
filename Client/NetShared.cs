/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;
using System.IO.Compression;

namespace ShiftDrive {

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