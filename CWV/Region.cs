using System.IO.Compression;
using static CWV.BigEndianStreams;
using static CWV.Compression;

namespace CWV;

internal class Region {
    const int SECTOR_LENGTH = 4096;
    public enum ChunkStatus {
        Overlapping = -5,
        MismatchedLengths = -4,
        ZeroLength = -3,
        InHeader = -2,
        OutOfFile = -1,
        Ok = 0,
        NotCreated = 1
    }
    public class ChunkMetadata(byte x, byte z) {
        public byte X { get; } = x;
        public byte Z { get; } = z;
        public uint BlockStart { get; set; } = 0;
        public byte SectorCount { get; set; } = 0;
        public uint Timestamp { get; set; } = 0;
        public int Length { get; set; } = 0;
        public CompressionType compression = CompressionType.Uncompressed;
        public ChunkStatus status;

        public override string ToString() => $"{GetType().Name}({X}, {Z})";

        public string ToStringFull() => $"{GetType().Name}({X}, {Z}, sector={BlockStart}, blocklength={SectorCount}, timestamp={Timestamp}, bytelength={Length}, compression={compression}, status={status})";

        public int RequiredBlocks { get => (Length + 4) / SECTOR_LENGTH; }

        public bool IsCreated { get => BlockStart != 0; }

        public void UpdateStatus(long fileSize) {
            if (SectorCount == 0 && BlockStart == 0) status = ChunkStatus.NotCreated;
            else if (SectorCount == 0) status = ChunkStatus.ZeroLength;
            else if (BlockStart < 2 && BlockStart != 0) status = ChunkStatus.InHeader;
            else if (SECTOR_LENGTH * BlockStart + 5 > fileSize) status = ChunkStatus.OutOfFile;
            else status = ChunkStatus.Ok;
        }
    }

    public readonly struct Metadata : IEnumerable<ChunkMetadata> {
        readonly ChunkMetadata[] metadata = new ChunkMetadata[32 * 32];

        public Metadata() {
            for (byte x = 0; x < 0x20; x++) {
                for (byte z = 0; z < 0x20; z++) {
                    this[x, z] = new ChunkMetadata(x, z);
                }
            }
        }

        public override readonly string? ToString() => "[" + string.Join(", ", this) + "]";

        public readonly ChunkMetadata this[byte x, byte z] {
            get { return metadata[x * 32 + z]; }
            set { metadata[x * 32 + z] = value; }
        }

        public readonly ChunkMetadata this[int i] {
            get { return metadata[i]; }
            set { metadata[i] = value; }
        }

        public readonly IEnumerator<ChunkMetadata> GetEnumerator() {
            return ((IEnumerable<ChunkMetadata>)metadata).GetEnumerator();
        }

        readonly System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    public class RegionFile {
        long fileSize = 0;
        public Metadata metadata = new();
        CompressionType compression;
        public static RegionFile FromFile(string fp) {
            var region = new RegionFile();
            FileStream stream = File.OpenRead(fp);
            stream.Seek(0, SeekOrigin.End);
            region.fileSize = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new BinaryReader2(stream);
            //header.metadata = metadata;
            region.ReadHeader(reader);
            region.ReadChunks(stream);
            return region;
        }

        public void ReadHeader(BinaryReader2 reader) {
            if (fileSize == 0) return;
            if (fileSize < 2 * SECTOR_LENGTH) throw new FormatException("The region file does not have header.");
            for (int i = 0; i < SECTOR_LENGTH / 4; i++) {
                metadata[i].BlockStart = reader.ReadUInt24();
                metadata[i].SectorCount = reader.ReadByte();
                metadata[i].UpdateStatus(fileSize);
            }
            for (int i = 0; i < SECTOR_LENGTH / 4; i++) {
                metadata[i].Timestamp = reader.ReadUInt32();
                Console.WriteLine(metadata[i].ToStringFull());
            }
            Console.WriteLine(metadata.ToString());
        }

        public void ReadChunks(Stream stream) {
            var reader = new BinaryReader2(stream);
            int length = (int)reader.ReadUInt32();
            Console.WriteLine(length);
            compression = (CompressionType)reader.ReadByte();
            var decompressedReader = new BinaryReader2(stream);
            if (compression == CompressionType.GZipCompressed) {
                decompressedReader = new(new GZipStream(stream, CompressionMode.Decompress));
            }
            else if (compression == CompressionType.ZLibCompressed) {
                decompressedReader = new(new ZLibStream(stream, CompressionMode.Decompress));
            }
            Console.WriteLine(stream.Length);   // 8197
            Console.WriteLine(stream.Position); // 7962624
            foreach (string key in Nbt.NBTFile.FromReaderUncompressed(decompressedReader).Keys) {
                Console.WriteLine(key);
            }
            //File.WriteAllText("D:\\chunk.txt", Nbt.NBTFile.FromReaderUncompressed(decompressedReader).PrettyTree());
            Console.WriteLine(stream.Length);   // 7962624
            Console.WriteLine(stream.Position); // 16389
            decompressedReader.ReadByte(); // он умер от смерти от смерти от смерти и калий который я сьел во вторник в шкиле 🦈
        }
    }
}
