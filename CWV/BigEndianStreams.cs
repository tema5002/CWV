namespace CWV;

static internal class BigEndianStreams {

    public class BinaryReader2(Stream stream) : BinaryReader(stream, System.Text.Encoding.UTF8, true) {
        public Span<byte> ReadBytesReversed(int count) {
            var data = base.ReadBytes(count);
            Array.Reverse(data);
            return new Span<byte>(data);
        }
        public override short ReadInt16() {
            return BitConverter.ToInt16(ReadBytesReversed(2));
        }

        public override ushort ReadUInt16() {
            return BitConverter.ToUInt16(ReadBytesReversed(2));
        }

        public uint ReadUInt24() {
            return (uint)ReadByte() << 16 | (uint)ReadByte() << 8 | ReadByte();
        }

        public override uint ReadUInt32() {
            return BitConverter.ToUInt32(ReadBytesReversed(4));
        }

        public override int ReadInt32() {
            return BitConverter.ToInt32(ReadBytesReversed(4));
        }

        public override long ReadInt64() {
            return BitConverter.ToInt64(ReadBytesReversed(8));
        }

        public override float ReadSingle() {
            return BitConverter.ToSingle(ReadBytesReversed(4));
        }

        public override double ReadDouble() {
            return BitConverter.ToDouble(ReadBytesReversed(8));
        }
    }

    public class BinaryWriter2(Stream stream) : BinaryWriter(stream) {
        public override void Write(short value) {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }

        public override void Write(ushort value) {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }

        public void WriteUInt24(uint value) {
            Write((byte)value);
            Write((byte)(value >> 8));
            Write((byte)(value >> 16));
        }

        public override void Write(uint value) {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }

        public override void Write(int value) {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }

        public override void Write(long value) {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }

        public override void Write(float value) {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }

        public override void Write(double value) {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }

        public void Write(CWV.Nbt.TAG_ID value) {
            base.Write((byte)value);
        }

        public void Write(CWV.Compression.CompressionType value) {
            base.Write((byte)value);
        }
    }
}
