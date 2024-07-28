using System.Collections;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace CWV;

internal class Nbt {
    public enum TAG_ID : byte {
        TAG_End,
        TAG_Byte,
        TAG_Short,
        TAG_Int,
        TAG_Long,
        TAG_Float,
        TAG_Double,
        TAG_Byte_Array,
        TAG_String,
        TAG_List,
        TAG_Compound,
        TAG_Int_Array,
        TAG_Long_Array
    }

    public interface ITag {
        TAG_ID Id { get; }
        string Name { get; set; }
        string TagInfo();
        string ValueString();
        string PrettyTree(int indent);
    }

    private static readonly Dictionary<char, string> ESCAPE_DICT = new Dictionary<char, string>() {
        {'\\', "\\\\"},
        {'"', "\\\""},
        {'\x00', "\\u0000"},
        {'\x01', "\\u0001"},
        {'\x02', "\\u0002"},
        {'\x03', "\\u0003"},
        {'\x04', "\\u0004"},
        {'\x05', "\\u0005"},
        {'\x06', "\\u0006"},
        {'\x07', "\\u0007"},
        {'\b', "\\b"},
        {'\t', "\\t"},
        {'\n', "\\n"},
        {'\x0b', "\\u000b"},
        {'\f', "\\f"},
        {'\r', "\\r"},
        {'\x0e', "\\u000e"},
        {'\x0f', "\\u000f"},
        {'\x10', "\\u0010"},
        {'\x11', "\\u0011"},
        {'\x12', "\\u0012"},
        {'\x13', "\\u0013"},
        {'\x14', "\\u0014"},
        {'\x15', "\\u0015"},
        {'\x16', "\\u0016"},
        {'\x17', "\\u0017"},
        {'\x18', "\\u0018"},
        {'\x19', "\\u0019"},
        {'\x1a', "\\u001a"},
        {'\x1b', "\\u001b"},
        {'\x1c', "\\u001c"},
        {'\x1d', "\\u001d"},
        {'\x1e', "\\u001e"},
        {'\x1f', "\\u001f"}
    };
    private static readonly Regex ESCAPE = new("[\\x00-\\x1f\\\\\"\\b\\f\\n\\r\\t]");
    private static string AsciiEscape(string s) {
        return '"' + ESCAPE.Replace(s, m => ESCAPE_DICT[m.Value[0]]) + '"';
    }

    public abstract class TAG<T>(string name, T value) : ITag {
        public abstract TAG_ID Id { get; }
        public string Name { get; set; } = name;
        public T Value { get; set; } = value;

        public string TagInfo() {
            return GetType().Name + (Name == "" ? "" : $"({AsciiEscape(Name)})") + ": " + ValueString();
        }

        public virtual string ValueString() {
            string? value = Value?.ToString();
            if (Value is string) value = AsciiEscape(value!);
            return value!;
        }

        public virtual string PrettyTree(int indent = 0) {
            return new string('\t', indent) + TagInfo();
        }
    }

    public abstract class TAG_Array<T> : TAG<List<T>>, IList<T> {
        public virtual string? TypeOfT { get; } // i would make it abstract but TAG_Compound/TAG_List does not use it

        public TAG_Array(string name, List<T> value) : base(name, value) { }

        public override string ValueString() => "[" + $"{Value.Count} {TypeOfT}" + (Count > 1 ? "s" : "") + "]";

        public override string ToString() => "[" + string.Join(", ", Value) + "]";

        public void Add(T b) => Value.Add(b);

        public void Clear() => Value.Clear();

        public bool Contains(T b) => Value.Contains(b);

        public void CopyTo(T[] array, int arrayIndex) => CopyTo(array, arrayIndex);

        public int Count { get => Value.Count; }

        public bool Empty { get => Count == 0; }

        public bool IsReadOnly => false;

        public T this[int index] {
            get { return Value[index]; }
            set { Value[index] = value; }
        }

        public bool Remove(T b) => Value.Remove(b);

        IEnumerator IEnumerable.GetEnumerator() => Value.GetEnumerator();

        public IEnumerator<T> GetEnumerator() => Value.GetEnumerator();

        public int IndexOf(T b) => Value.IndexOf(b);

        public void Insert(int index, T item) => Value.Insert(index, item);

        public void RemoveAt(int index) => Value.RemoveAt(index);
        /*
        public override string PrettyTree(int indent = 0) {
            string output = base.PrettyTree(indent);
            if (!Empty) {
                output += "\n" + new string('\t', indent) + "{";
                foreach (T t in Value) {
                    output += new string('\t', indent) + "\n" + t?.ToString();
                }
                output += "\n" + new string('\t', indent) + "}";
            }
            return output;
        }*/
    }

    public class TAG_End : TAG<object?> {
        public override TAG_ID Id => TAG_ID.TAG_End;
        // if you will use name or value i will send you to austria

        public TAG_End() : base("", null) { }
    }

    public class TAG_Byte : TAG<byte> {
        public override TAG_ID Id => TAG_ID.TAG_Byte;
        public TAG_Byte(string name, byte value) : base(name, value) { }
    }

    public class TAG_Short : TAG<short> {
        public override TAG_ID Id => TAG_ID.TAG_Short;
        public TAG_Short(string name, short value) : base(name, value) { }
    }

    public class TAG_Int : TAG<int> {
        public override TAG_ID Id => TAG_ID.TAG_Int;
        public TAG_Int(string name, int value) : base(name, value) { }
    }

    public class TAG_Long : TAG<long> {
        public override TAG_ID Id => TAG_ID.TAG_Long;
        public TAG_Long(string name, long value) : base(name, value) { }
    }

    public class TAG_Float : TAG<float> {
        public override TAG_ID Id => TAG_ID.TAG_Float;
        public TAG_Float(string name, float value) : base(name, value) { }

        public override string ValueString() {
            if (float.IsNaN(Value)) return "nan";
            if (float.IsInfinity(Value)) return "inf";
            if (float.IsNegativeInfinity(Value)) return "-inf";
            return base.ValueString();
        }
    }

    public class TAG_Double : TAG<double> {
        public override TAG_ID Id => TAG_ID.TAG_Double;
        public TAG_Double(string name, double value) : base(name, value) { }

        public override string ValueString() {
            if (double.IsNaN(Value)) return "nan";
            if (double.IsInfinity(Value)) return "inf";
            if (double.IsNegativeInfinity(Value)) return "-inf";
            return base.ValueString();
        }
    }

    public class TAG_Byte_Array : TAG_Array<sbyte> {
        public override TAG_ID Id => TAG_ID.TAG_Byte_Array;
        public override string TypeOfT => "byte";

        public TAG_Byte_Array(string name, List<sbyte> value) : base(name, value) { }
    }

    public class TAG_String : TAG<string> {
        public override TAG_ID Id => TAG_ID.TAG_String;
        public TAG_String(string name, string value) : base(name, value) { }

        public char this[int index] {
            get { return Value[index]; }
        }

        public int Length { get => Value.Length; }
        public bool Contains(char value) => Value.Contains(value);
        public bool Contains(string value) => Value.Contains(value);
    }

    public class TAG_List : TAG_Array<ITag> {
        public override TAG_ID Id => TAG_ID.TAG_List;
        public TAG_ID tagsId;
        public override string TypeOfT => "TAG";

        public TAG_List(string name, List<ITag> value, TAG_ID tagsId) : base(name, value) {
            this.tagsId = tagsId;
        }

        public override string ValueString() => $"{Value.Count} entries of type {tagsId}";

        public override string PrettyTree(int indent = 0) {
            string output = base.PrettyTree(indent);
            if (!Empty) {
                output += "\n" + new string('\t', indent) + "{";
                foreach (ITag? tag in Value) {
                    output += "\n" + tag?.PrettyTree(indent + 1);
                }
                output += "\n" + new string('\t', indent) + "}";
            }
            return output;
        }
    }

    public class TAG_Compound : TAG_Array<ITag> {
        public override TAG_ID Id => TAG_ID.TAG_Compound;
        public override string TypeOfT => "TAG";

        public TAG_Compound(string name, List<ITag> value) : base(name, value) { }

        public override string ToString() {
            return "{" + string.Join(", ", from tag in Value select tag.TagInfo()) + "}";
        }

        public override string ValueString() => $"{Count} Entr" + (Count > 1 ? "ies" : "y");

        public ITag this[string key] {
            get {
                foreach (ITag tag in Value) {
                    if (tag.Name == key) {
                        return tag;
                    }
                }
                throw new KeyNotFoundException($"Tag {key} does not exist");
            }
            set {
                value.Name = key;
                for (int i = 0; i < Count; i++) {
                    if (this[i].Name == key) {
                        this[i] = value;
                        return;
                    }
                }
                Add(value);
            }
        }

        public bool ContainsKey(string key) {
            foreach (ITag tag in Value) {
                if (tag.Name == key) {
                    return true;
                }
            }
            return false;
        }

        public bool Remove(string key) => Value.Remove(this[key]);

        public IEnumerable<string> Keys => from tag in Value select tag.Name;

        public override string PrettyTree(int indent = 0) {
            string output = base.PrettyTree(indent);
            if (!Empty) {
                output += "\n" + new string('\t', indent) + "{";
                foreach (ITag? tag in Value) {
                    output += "\n" + tag?.PrettyTree(indent + 1);
                }
                output += "\n" + new string('\t', indent) + "}";
            }
            return output;
        }
    }

    public class TAG_Int_Array : TAG_Array<int> {
        public override TAG_ID Id => TAG_ID.TAG_Int_Array;
        public override string TypeOfT => "int";

        public TAG_Int_Array(string name, List<int> value) : base(name, value) { }
    }

    public class TAG_Long_Array : TAG_Array<long> {
        public override TAG_ID Id => TAG_ID.TAG_Long_Array;
        public override string TypeOfT => "long";

        public TAG_Long_Array(string name, List<long> value) : base(name, value) { }
    }

    private class BinaryReader2(Stream stream) : BinaryReader(stream) {
        public override short ReadInt16() {
            var data = base.ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public override ushort ReadUInt16() {
            var data = base.ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public override uint ReadUInt32() {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public override int ReadInt32() {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public override long ReadInt64() {
            var data = base.ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        public override float ReadSingle() {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToSingle(data, 0);
        }

        public override double ReadDouble() {
            var data = base.ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToDouble(data, 0);
        }
    }

    private class BinaryWriter2(Stream stream) : BinaryWriter(stream) {
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

        public void Write(TAG_ID value) {
            base.Write((byte)value);
        }
    }

    public enum CompressionType {
        Uncompressed,
        GZipCompressed,
        ZLibCompressed
    }

    public class NBTFile : TAG_Compound {
        public NBTFile(string name, List<ITag> value) : base(name, value) { }

        private static string ReadString(BinaryReader2 reader) {
            ushort length = reader.ReadUInt16();
            string s = Encoding.UTF8.GetString(reader.ReadBytes(length));
            return s;
        }

        private static TAG_List ReadList(string name, BinaryReader2 reader) {
            TAG_ID id = (TAG_ID)reader.ReadByte();
            int length = reader.ReadInt32();
            var list = new List<ITag>(length);

            for (int i = 0; i < length; i++) {
                list.Add(ReadTag(id, "", reader));
            }

            return new TAG_List(name, list, id);
        }

        private static TAG_Compound ReadCompound(string name, BinaryReader2 reader) {
            var tags = new List<ITag>();
            while (true) {
                ITag tag = ReadTagWithPrefix(reader);
                if (tag.Id == TAG_ID.TAG_End) break;
                tags.Add(tag);
            }
            return new TAG_Compound(name, tags);
        }

        private static TAG_Byte_Array ReadByteArray(string name, BinaryReader2 reader) {
            int length = reader.ReadInt32();
            var list = new List<sbyte>(length);

            for (int i = 0; i < length; i++) {
                list.Add(reader.ReadSByte());
            }

            return new TAG_Byte_Array(name, list);
        }

        private static TAG_Int_Array ReadIntArray(string name, BinaryReader2 reader) {
            int length = reader.ReadInt32();
            var list = new List<int>(length);

            for (int i = 0; i < length; i++) {
                list.Add(reader.ReadInt32());
            }

            return new TAG_Int_Array(name, list);
        }

        private static TAG_Long_Array ReadLongArray(string name, BinaryReader2 reader) {
            int length = reader.ReadInt32();
            var list = new List<long>(length);

            for (int i = 0; i < length; i++) {
                list.Add(reader.ReadInt64());
            }

            return new TAG_Long_Array(name, list);
        }

        private static ITag ReadTag(TAG_ID id, string name, BinaryReader2 reader) {
            switch (id) {
                case TAG_ID.TAG_End:
                    return new TAG_End();
                case TAG_ID.TAG_Byte:
                    return new TAG_Byte(name, reader.ReadByte());
                case TAG_ID.TAG_Short:
                    return new TAG_Short(name, reader.ReadInt16());
                case TAG_ID.TAG_Int:
                    return new TAG_Int(name, reader.ReadInt32());
                case TAG_ID.TAG_Long:
                    return new TAG_Long(name, reader.ReadInt64());
                case TAG_ID.TAG_Float:
                    return new TAG_Float(name, reader.ReadSingle());
                case TAG_ID.TAG_Double:
                    return new TAG_Double(name, reader.ReadDouble());
                case TAG_ID.TAG_Byte_Array:
                    return ReadByteArray(name, reader);
                case TAG_ID.TAG_String:
                    return new TAG_String(name, ReadString(reader));
                case TAG_ID.TAG_List:
                    return ReadList(name, reader);
                case TAG_ID.TAG_Compound:
                    return ReadCompound(name, reader);
                case TAG_ID.TAG_Int_Array:
                    return ReadIntArray(name, reader);
                case TAG_ID.TAG_Long_Array:
                    return ReadLongArray(name, reader);
                default:
                    throw new NotSupportedException($"Unknown tag type: {id}");
            }
        }

        private static ITag ReadTagWithPrefix(BinaryReader2 reader) {
            TAG_ID id = (TAG_ID)reader.ReadByte();
            string name = id == TAG_ID.TAG_End ? "" : ReadString(reader);
            return ReadTag(id, name, reader);
        }

        public static NBTFile FromStream(Stream stream) {
            byte[] headerBytes = new byte[3];
            stream.Read(headerBytes, 0, 3);
            stream.Seek(0, SeekOrigin.Begin);

            Stream decompressedStream;
            if (headerBytes[0] == 0x0A) {
                // no compression
                decompressedStream = stream;
            }
            else if (headerBytes[0] == 0x1F && headerBytes[1] == 0x8B) {
                // GZIP
                decompressedStream = new GZipStream(stream, CompressionMode.Decompress);
            }
            else if (headerBytes[0] == 0x78 && (headerBytes[1] == 0x9C || headerBytes[1] == 0xDA)) {
                // ZLIB
                decompressedStream = new ZLibStream(stream, CompressionMode.Decompress);
            }
            else {
                throw new FormatException("Unknown compression type.");
            }

            using (var reader = new BinaryReader2(decompressedStream)) {
                var tags = ReadTagWithPrefix(reader);
                if (tags.Id == TAG_ID.TAG_Compound) return new NBTFile(tags.Name, ((TAG_Compound)tags).Value);
                throw new FormatException("The file is not TAG_Compound.");
            }
        }

        public static NBTFile FromFile(string filePath) {
            return FromStream(File.OpenRead(filePath));
        }

        private static void WriteString(string s, BinaryWriter2 writer) {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            writer.Write((ushort)bytes.Length);
            writer.Write(bytes);
        }

        private static void WriteList(TAG_List list, BinaryWriter2 writer) {
            writer.Write(list.tagsId);
            writer.Write(list.Count);

            foreach (ITag tag in list) {
                if (tag.Id != list.tagsId) throw new ArgumentException($"Expected {list.tagsId} in TAG_List, not {tag.Id}");
                WriteTag(tag, writer);
            }
        }

        private static void WriteCompound(TAG_Compound compound, BinaryWriter2 writer) {
            foreach (ITag tag in compound) {
                WriteTagWithPrefix(tag, writer);
            }
            writer.Write(TAG_ID.TAG_End);
        }

        private static void WriteByteArray(TAG_Byte_Array byteArray, BinaryWriter2 writer) {
            writer.Write(byteArray.Count);
            
            foreach (sbyte b in byteArray) {
                writer.Write(b);
            }
        }

        private static void WriteIntArray(TAG_Int_Array intArray, BinaryWriter2 writer) {
            writer.Write(intArray.Count);

            foreach (int i in intArray) {
                writer.Write(i);
            }
        }

        private static void WriteLongArray(TAG_Long_Array longArray, BinaryWriter2 writer) {
            writer.Write(longArray.Count);

            foreach (long l in longArray) {
                writer.Write(l);
            }
        }

        private static void WriteTag(ITag tag, BinaryWriter2 writer) {
            switch (tag.Id) {
                case TAG_ID.TAG_Byte:
                    writer.Write(((TAG_Byte)tag).Value);
                    break;
                case TAG_ID.TAG_Short:
                    writer.Write(((TAG_Short)tag).Value);
                    break;
                case TAG_ID.TAG_Int:
                    writer.Write(((TAG_Int)tag).Value);
                    break;
                case TAG_ID.TAG_Long:
                    writer.Write(((TAG_Long)tag).Value);
                    break;
                case TAG_ID.TAG_Float:
                    writer.Write(((TAG_Float)tag).Value);
                    break;
                case TAG_ID.TAG_Double:
                    writer.Write(((TAG_Double)tag).Value);
                    break;
                case TAG_ID.TAG_Byte_Array:
                    WriteByteArray((TAG_Byte_Array)tag, writer);
                    break;
                case TAG_ID.TAG_String:
                    WriteString(((TAG_String)tag).Value, writer);
                    break;
                case TAG_ID.TAG_List:
                    WriteList((TAG_List)tag, writer);
                    break;
                case TAG_ID.TAG_Compound:
                    WriteCompound((TAG_Compound)tag, writer);
                    break;
                case TAG_ID.TAG_Int_Array:
                    WriteIntArray((TAG_Int_Array)tag, writer);
                    break;
                case TAG_ID.TAG_Long_Array:
                    WriteLongArray((TAG_Long_Array)tag, writer);
                    break;
                default:
                    throw new NotSupportedException($"Unknown tag type: {tag.Id}");
            }
        }


        private static void WriteTagWithPrefix(ITag tag, BinaryWriter2 writer) {
            writer.Write(tag.Id);
            WriteString(tag.Name, writer);
            WriteTag(tag, writer);
        }


        public void ToStream(Stream stream, CompressionType compression = CompressionType.GZipCompressed) {
            if (compression == CompressionType.GZipCompressed) {
                stream = new GZipStream(stream, CompressionMode.Compress);
            }
            if (compression == CompressionType.ZLibCompressed) {
                stream = new ZLibStream(stream, CompressionMode.Compress);
            }

            using (var writer = new BinaryWriter2(stream)) {
                WriteTagWithPrefix(this, writer);
            }
        }

        public void ToFile(string filePath, CompressionType compression = CompressionType.GZipCompressed) {
            ToStream(File.Open(filePath, FileMode.Create), compression);
        }

        public override string PrettyTree(int indent = 0) {
            return new TAG_Compound(Name, Value).PrettyTree(indent);
        }
    }
}