using System.IO.Compression;
using System.Collections;
using static CWV.BigEndianStreams;
using static CWV.Compression;
using static CWV.Nbt;
using System.Linq;

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
    public class Chunk(byte x, byte z) {
        public NBTFile? Value { get; set; }

        // metadata
        public byte X { get; } = x;
        public byte Z { get; } = z;
        public uint Offset { get; set; } = 0;
        public byte SectorCount { get; set; } = 0;
        public uint Timestamp { get; set; } = 0;
        public int Length { get; set; } = 0;
        public CompressionType Compression = CompressionType.Uncompressed;
        public ChunkStatus status;
        public int RequiredBlocks { get => (Length + 4) / SECTOR_LENGTH; }
        public bool IsCreated { get => Offset != 0; }

        public override string ToString() => $"{GetType().Name}({X}, {Z})";

        public string ToStringFull() => $"{GetType().Name}({X}, {Z}, sector={Offset}, blocklength={SectorCount}, timestamp={Timestamp}, bytelength={Length}, compression={Compression}, status={status})";

        public void UpdateStatus(long fileSize) {
            if (SectorCount == 0 && Offset == 0) status = ChunkStatus.NotCreated;
            else if (SectorCount == 0) status = ChunkStatus.ZeroLength;
            else if (Offset < 2 && Offset != 0) status = ChunkStatus.InHeader;
            else if (SECTOR_LENGTH * Offset + 5 > fileSize) status = ChunkStatus.OutOfFile;
            else status = ChunkStatus.Ok;
        }
    }

    public class RegionFile : IEnumerable<Chunk> {
        long fileSize = 0;
        readonly Chunk[] chunks = new Chunk[32 * 32];

        // reader
        public static RegionFile FromFile(string fp) {
            var region = new RegionFile();
            FileStream stream = File.OpenRead(fp);
            stream.Seek(0, SeekOrigin.End);
            region.fileSize = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new BinaryReader2(stream);

            region.InitializeChunks();
            region.ReadHeader(reader);
            region.ReadChunks(stream);
            return region;
        }

        private void InitializeChunks() {
            for (byte x = 0; x < 0x20; x++) {
                for (byte z = 0; z < 0x20; z++) {
                    this[x, z] = new Chunk(x, z);
                }
            }
        }

        public void ReadHeader(BinaryReader2 reader) {
            if (fileSize == 0) return;
            if (fileSize < 2 * SECTOR_LENGTH) throw new FormatException("The region file does not have header.");
            for (int i = 0; i < SECTOR_LENGTH / 4; i++) {
                chunks[i].Offset = reader.ReadUInt24();
                chunks[i].SectorCount = reader.ReadByte();
                chunks[i].UpdateStatus(fileSize);
            }
            for (int i = 0; i < SECTOR_LENGTH / 4; i++) {
                chunks[i].Timestamp = reader.ReadUInt32();
                Console.WriteLine(chunks[i].ToStringFull());
            }
            Console.WriteLine(chunks.ToString());
        }

        private static byte[] DecompressChunk(byte[] bytes, CompressionType compression) {
            if (compression == CompressionType.GZipCompressed) {
                using var ms = new MemoryStream(bytes);
                using var gzip = new GZipStream(ms, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gzip.CopyTo(output);
                return output.ToArray();
            }
            else if (compression == CompressionType.ZLibCompressed) {
                using var ms = new MemoryStream(bytes);
                using var zlib = new ZLibStream(ms, CompressionMode.Decompress);
                using var output = new MemoryStream();
                zlib.CopyTo(output);
                return output.ToArray();
            }
            return bytes;
        }

        public void ReadChunks(Stream stream) {
            var reader = new BinaryReader2(stream);
            for (int i = 0; i < 32 * 32; i++) {
                stream.Seek(chunks[i].Offset * SECTOR_LENGTH, SeekOrigin.Begin);
                chunks[i].Length = (int)reader.ReadUInt32();
                chunks[i].Compression = (CompressionType)reader.ReadByte();
                
                if (chunks[i].Offset != 0 && chunks[i].Length != 0) {
                    chunks[i].Value = NBTFile.FromBytes(DecompressChunk(reader.ReadBytes(chunks[i].Length), chunks[i].Compression));
                }
            }
        }

        // utilities and other things to work with class
        public override string? ToString() => "[" + string.Join(", ", this) + "]";

        public Chunk this[byte x, byte z] {
            get { return chunks[x * 32 + z]; }
            set { chunks[x * 32 + z] = value; }
        }

        public Chunk this[int i] {
            get { return chunks[i]; }
            set { chunks[i] = value; }
        }

        public IEnumerator<Chunk> GetEnumerator() => ((IEnumerable<Chunk>)chunks).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public List<byte[]> GetChunksCoords() {
            List<byte[]> coords = [];
            for (byte x = 0; x < 0x20; x++) {
                for (byte z = 0; z < 0x20; z++) {
                    if (this[x, z].IsCreated) {
                        coords.Add([x, z]);
                    }
                }
            }
            return coords;
        }

        public List<Chunk> GetChunks() {
            List<Chunk> chunks = [];
            foreach (byte[] chunkCoords in GetChunksCoords()) {
                chunks.Add(this[chunkCoords[0], chunkCoords[1]]);
            }
            return chunks;
        }
    }
}
