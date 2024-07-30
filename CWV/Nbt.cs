using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using static CWV.BigEndianStreams;
using static CWV.Compression;

namespace CWV;

internal partial class Nbt {
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

    private static readonly Dictionary<char, string> ESCAPE_DICT = new() {
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

    public abstract class TAG_Array<T>(string name, List<T> value) : TAG<List<T>>(name, value), IList<T> {
        public virtual string? TypeOfT { get; } // i would make it abstract but TAG_Compound/TAG_List does not use it

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

    public class TAG_Byte(string name, byte value) : TAG<byte>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Byte;
    }

    public class TAG_Short(string name, short value) : TAG<short>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Short;
    }

    public class TAG_Int(string name, int value) : TAG<int>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Int;
    }

    public class TAG_Long(string name, long value) : TAG<long>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Long;
    }

    public class TAG_Float(string name, float value) : TAG<float>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Float;

        public override string ValueString() {
            if (float.IsNaN(Value)) return "nan";
            if (float.IsInfinity(Value)) return "inf";
            if (float.IsNegativeInfinity(Value)) return "-inf";
            return base.ValueString();
        }
    }

    public class TAG_Double(string name, double value) : TAG<double>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Double;

        public override string ValueString() {
            if (double.IsNaN(Value)) return "nan";
            if (double.IsInfinity(Value)) return "inf";
            if (double.IsNegativeInfinity(Value)) return "-inf";
            return base.ValueString();
        }
    }

    public class TAG_Byte_Array(string name, List<sbyte> value) : TAG_Array<sbyte>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Byte_Array;
        public override string TypeOfT => "byte";
    }

    public class TAG_String(string name, string value) : TAG<string>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_String;

        public char this[int index] {
            get { return Value[index]; }
        }

        public int Length { get => Value.Length; }
        public bool Contains(char value) => Value.Contains(value);
        public bool Contains(string value) => Value.Contains(value);
    }

    public class TAG_List(string name, List<Nbt.ITag> value, Nbt.TAG_ID tagsId) : TAG_Array<ITag>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_List;
        public TAG_ID tagsId = tagsId;
        public override string TypeOfT => "TAG";

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

    public class TAG_Compound(string name, List<Nbt.ITag> value) : TAG_Array<ITag>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Compound;
        public override string TypeOfT => "TAG";

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

    public class TAG_Int_Array(string name, List<int> value) : TAG_Array<int>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Int_Array;
        public override string TypeOfT => "int";
    }

    public class TAG_Long_Array(string name, List<long> value) : TAG_Array<long>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Long_Array;
        public override string TypeOfT => "long";
    }

    public class NBTFile(string name, List<Nbt.ITag> value) : TAG_Compound(name, value) {
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
            return id switch {
                TAG_ID.TAG_End => new TAG_End(),
                TAG_ID.TAG_Byte => new TAG_Byte(name, reader.ReadByte()),
                TAG_ID.TAG_Short => new TAG_Short(name, reader.ReadInt16()),
                TAG_ID.TAG_Int => new TAG_Int(name, reader.ReadInt32()),
                TAG_ID.TAG_Long => new TAG_Long(name, reader.ReadInt64()),
                TAG_ID.TAG_Float => new TAG_Float(name, reader.ReadSingle()),
                TAG_ID.TAG_Double => new TAG_Double(name, reader.ReadDouble()),
                TAG_ID.TAG_Byte_Array => ReadByteArray(name, reader),
                TAG_ID.TAG_String => new TAG_String(name, ReadString(reader)),
                TAG_ID.TAG_List => ReadList(name, reader),
                TAG_ID.TAG_Compound => ReadCompound(name, reader),
                TAG_ID.TAG_Int_Array => ReadIntArray(name, reader),
                TAG_ID.TAG_Long_Array => ReadLongArray(name, reader),
                _ => throw new NotSupportedException($"Unknown tag type: {id}"),
            };
        }

        private static ITag ReadTagWithPrefix(BinaryReader2 reader) {
            TAG_ID id = (TAG_ID)reader.ReadByte();
            string name = id == TAG_ID.TAG_End ? "" : ReadString(reader);
            return ReadTag(id, name, reader);
        }

        public static NBTFile FromReaderUncompressed(BinaryReader2 reader) {
            var tags = ReadTagWithPrefix(reader);
            if (tags.Id == TAG_ID.TAG_Compound) return new NBTFile(tags.Name, ((TAG_Compound)tags).Value);
            throw new FormatException("The file is not TAG_Compound.");
        }

        public static NBTFile FromBytes(byte[] bytes) {
            Stream stream = new MemoryStream(bytes);
            if (bytes[0] == 0x0A) {
                // no compression
            }
            else if (bytes[0] == 0x1F && bytes[1] == 0x8B) {
                // GZIP
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }
            else if (bytes[0] == 0x78 && (bytes[1] == 0x9C || bytes[1] == 0xDA)) {
                // ZLIB
                stream = new ZLibStream(stream, CompressionMode.Decompress);
            }
            else {
                throw new FormatException("Unknown compression type.");
            }

            return FromReaderUncompressed(new BinaryReader2(stream));
        }

        public static NBTFile FromStream(Stream stream, int offset, int count) {
            byte[] buffer = new byte[count];
            stream.Read(buffer, offset, count);
            return FromBytes(buffer);
        }

        public static NBTFile FromFile(string filePath) {
            using Stream stream = File.OpenRead(filePath);
            int fileSize = (int)stream.Length;
            return FromStream(stream, 0, fileSize);
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

            using var writer = new BinaryWriter2(stream);
            WriteTagWithPrefix(this, writer);
        }

        public void ToFile(string filePath, CompressionType compression = CompressionType.GZipCompressed) {
            ToStream(File.Open(filePath, FileMode.Create), compression);
        }

        public override string PrettyTree(int indent = 0) {
            return new TAG_Compound(Name, Value).PrettyTree(indent);
        }
    }
}