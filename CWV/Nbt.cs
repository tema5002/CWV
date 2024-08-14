using System.Collections;
using System.IO.Compression;
using System.Text;
using static CWV.BigEndianStreams;

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
    }

    public abstract class TAG<T>(string name, T value) : ITag {
        public abstract TAG_ID Id { get; }
        public string Name { get; set; } = name;
        public T Value { get; set; } = value;
    }

    public abstract class TAG_Array<T>(string name, List<T> value) : TAG<List<T>>(name, value), IEnumerable<T> {
        public int Count { get => Value.Count; }

        IEnumerator IEnumerable.GetEnumerator() => Value.GetEnumerator();

        public IEnumerator<T> GetEnumerator() => Value.GetEnumerator();
    }

    public class TAG_End() : TAG<object?>("", null) {
        public override TAG_ID Id => TAG_ID.TAG_End;
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
    }

    public class TAG_Double(string name, double value) : TAG<double>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Double;
    }

    public class TAG_Byte_Array(string name, List<sbyte> value) : TAG_Array<sbyte>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Byte_Array;
    }

    public class TAG_String(string name, string value) : TAG<string>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_String;
    }

    public class TAG_List(string name, List<Nbt.ITag> value, TAG_ID tagsId) : TAG_Array<ITag>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_List;
        public TAG_ID tagsId = tagsId;
    }

    public class TAG_Compound(string name, List<Nbt.ITag> value) : TAG_Array<ITag>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Compound;
    }

    public class TAG_Int_Array(string name, List<int> value) : TAG_Array<int>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Int_Array;
    }

    public class TAG_Long_Array(string name, List<long> value) : TAG_Array<long>(name, value) {
        public override TAG_ID Id => TAG_ID.TAG_Long_Array;
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

        public byte[] ToBytes() {
            using var memoryStream = new MemoryStream();
            using var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress);
            WriteTagWithPrefix(this, new BinaryWriter2(gzipStream));
            return memoryStream.ToArray();
        }
    }
}