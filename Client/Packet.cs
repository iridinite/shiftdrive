using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ShiftDrive {

    /// <summary>
    /// Describes the function of a packet.
    /// </summary>
    internal enum PacketID : byte {
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
    /// Represents a networking packet.
    /// </summary>
    internal sealed class Packet : IDisposable {

        private MemoryStream byteStream;
        private BinaryReader byteReader;
        private BinaryWriter byteWriter;
        private readonly PacketID id;

        private bool disposed = false;

        /// <summary>
        /// Constructs a new read-only <see cref="Packet"/> with a byte array received from the network.
        /// </summary>
        /// <param name="packetBytes">A byte array containing the entire packet.</param>
        public Packet(byte[] packetBytes) {
            if (packetBytes == null || packetBytes.Length < 2)
                throw new ArgumentException();

            // read metadata
            id = (PacketID)packetBytes[0];
            bool compress = packetBytes[1] > 0;
            // copy and decompress payload
            byte[] payload = new byte[packetBytes.Length - 2];
            Buffer.BlockCopy(packetBytes, 2, payload, 0, packetBytes.Length - 2);
            if (compress)
                payload = NetShared.DecompressBuffer(payload);
            // initialize the stream with the payload data
            byteStream = new MemoryStream(payload, false);
            byteReader = new BinaryReader(byteStream, Encoding.UTF8);
        }

        /// <summary>
        /// Constructs a new empty <see cref="Packet"/> for writing to.
        /// </summary>
        /// <param name="id">The <seealso cref="PacketID"/> to initialize the stream with.</param>
        public Packet(PacketID id) {
            this.id = id;
            byteStream = new MemoryStream();
            byteWriter = new BinaryWriter(byteStream, Encoding.UTF8);
        }

        /// <summary>
        /// Returns the <seealso cref="PacketID"/> assigned to this <see cref="Packet"/>.
        /// </summary>
        public PacketID GetID() {
            return id;
        }

        /// <summary>
        /// Returns the length of the stream in bytes.
        /// </summary>
        public long GetLength() {
            return byteStream.Length;
        }

        /// <summary>
        /// Converts the <seealso cref="Packet"/> to a byte array, for network transmission.
        /// </summary>
        public byte[] ToArray() {
            Debug.Assert(byteWriter != null, "Packet was received from network, should not be serialized again");

            using (MemoryStream outstream = new MemoryStream((int)GetLength() + 2)) {
                byte[] packetBytes = byteStream.ToArray();
                bool compress = packetBytes.Length > 1024;
                if (compress)
                    packetBytes = NetShared.CompressBuffer(packetBytes);

                outstream.WriteByte((byte)GetID());
                outstream.WriteByte(compress ? (byte)1 : (byte)0);
                outstream.Write(packetBytes, 0, packetBytes.Length);
                return outstream.ToArray();
            }
        }

        /// <summary>
        /// Writes a one-byte Boolean value to the current stream, with 0 representing false and 1 representing true.
        /// </summary>
        public void Write(bool value) {
            byteWriter.Write(value);
        }

        /// <summary>
        /// Writes an unsigned byte to the current stream and advances the stream position by one byte.
        /// </summary>
        public void Write(byte value) {
            byteWriter.Write(value);
        }

        /// <summary>
        /// Writes a byte array to the underlying stream.
        /// </summary>
        public void Write(byte[] value) {
            byteWriter.Write(value);
        }

        /// <summary>
        /// Writes a two-byte unsigned integer to the current stream and advances the stream position by two bytes.
        /// </summary>
        public void Write(ushort value) {
            byteWriter.Write(value);
        }

        /// <summary>
        /// Writes a four-byte unsigned integer to the current stream and advances the stream position by four bytes.
        /// </summary>
        public void Write(uint value) {
            byteWriter.Write(value);
        }

        /// <summary>
        /// Writes a four-byte signed integer to the current stream and advances the stream position by four bytes.
        /// </summary>
        public void Write(int value) {
            byteWriter.Write(value);
        }

        /// <summary>
        /// Writes a four-byte floating-point value to the current stream and advances the stream position by four bytes.
        /// </summary>
        public void Write(float value) {
            byteWriter.Write(value);
        }

        /// <summary>
        /// Writes a length-prefixed string to this stream in the current encoding of the
        /// <see cref="T:System.IO.BinaryWriter" />, and advances the current position of the stream in accordance
        /// with the encoding used and the specific characters being written to the stream.
        /// </summary>
        public void Write(string value) {
            byteWriter.Write(value);
        }

        /// <summary>
        /// Reads a Boolean value from the current stream and advances the current position of the stream by one byte.
        /// </summary>
        public bool ReadBoolean() {
            return byteReader.ReadBoolean();
        }

        /// <summary>
        /// Reads the next byte from the current stream and advances the current position of the stream by one byte.
        /// </summary>
        public byte ReadByte() {
            return byteReader.ReadByte();
        }

        /// <summary>
        /// Reads the specified number of bytes from the current stream into a byte array and advances the current position by that number of bytes.
        /// </summary>
        /// <returns>
        /// A byte array containing data read from the underlying stream. This might be less than the number of bytes requested if the end of the stream is reached.
        /// </returns>
        /// <param name="count">The number of bytes to read. This value must be 0 or a non-negative number or an exception will occur.</param>
        public byte[] ReadBytes(int count) {
            return byteReader.ReadBytes(count);
        }

        /// <summary>
        /// Reads a 2-byte unsigned integer from the current stream using little-endian encoding and advances the position of the stream by two bytes.
        /// </summary>
        public ushort ReadUInt16() {
            return byteReader.ReadUInt16();
        }

        /// <summary>
        /// Reads a 4-byte unsigned integer from the current stream and advances the position of the stream by four bytes.
        /// </summary>
        public uint ReadUInt32() {
            return byteReader.ReadUInt32();
        }

        /// <summary>
        /// Reads a 4-byte signed integer from the current stream and advances the position of the stream by four bytes.
        /// </summary>
        public int ReadInt32() {
            return byteReader.ReadInt32();
        }

        /// <summary>
        /// Reads a 4-byte floating point value from the current stream and advances the current position of the stream by four bytes.
        /// </summary>
        public float ReadSingle() {
            return byteReader.ReadSingle();
        }

        /// <summary>
        /// Reads a string from the current stream. The string is prefixed with the length, encoded as an integer seven bits at a time.
        /// </summary>
        public string ReadString() {
            return byteReader.ReadString();
        }

        /// <summary>
        /// Implements the <seealso cref="IDisposable"/> pattern.
        /// </summary>
        public void Dispose() {
            if (disposed) return;
            disposed = true;

            byteWriter?.Dispose();
            byteWriter = null;
            byteReader?.Dispose();
            byteReader = null;
            byteStream?.Dispose();
            byteStream = null;
        }

    }

}
