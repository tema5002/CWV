using System.IO.Compression;
using static CWV.BigEndianStreams;
using static CWV.Compression;
using static CWV.Nbt;

namespace CWV;

internal class Region {
    const int SECTOR_LENGTH = 4096;
    public enum ChunkStatus {
        Overlapping,
        MismatchedLengths,
        ZeroLength,
        InHeader,
        OutOfFile,
        Ok,
        NotCreated
    }
    public class Chunk(byte x, byte z) {
        public NBTFile? Value { get; set; }

        // metadata
        public byte X { get; } = x;
        public byte Z { get; } = z;
        public uint Offset { get; set; } = 0;
        public byte SectorCount { get; set; } = 0;
        public uint Timestamp { get; set; } = 0;
        public uint Length { get; set; } = 0;
        public CompressionType Compression = CompressionType.Uncompressed;
        public ChunkStatus status;
        public uint RequiredBlocks { get => (Length + 4) / SECTOR_LENGTH; }
        public bool IsCreated { get => Offset != 0; }

        public override string ToString() => $"{GetType().Name}({X}, {Z})";

        public string ToStringFull() => $"{GetType().Name}({X}, {Z}, sector={Offset}, blocklength={SectorCount}, timestamp={Timestamp}, bytelength={Length}, compression={Compression}, status={status})";
    }

    public class RegionFile {
        long fileSize = 0;
        readonly Chunk[] chunks = new Chunk[32 * 32];

        public static RegionFile FromFile(string fp) {
            var region = new RegionFile();
            using (var stream = File.OpenRead(fp)) {
                stream.Seek(0, SeekOrigin.End);
                region.fileSize = stream.Position;
                stream.Seek(0, SeekOrigin.Begin);

                region.ReadHeader(stream);
                region.ReadChunks(stream);
            }
            return region;
        }

        private void InitializeChunks() {
            for (byte x = 0; x < 0x20; x++) {
                for (byte z = 0; z < 0x20; z++) {
                    chunks[x * 32 + z] = new Chunk(x, z);
                }
            }
        }

        private void ReadHeader(Stream stream) {
            InitializeChunks();
            var reader = new BinaryReader2(stream);
            if (fileSize == 0) return;
            if (fileSize < 2 * SECTOR_LENGTH) throw new FormatException("The region file does not have header.");

            for (int i = 0; i < SECTOR_LENGTH; i += 4) {
                stream.Seek(i, SeekOrigin.Begin);
                chunks[i].Offset = reader.ReadUInt24();
                chunks[i].SectorCount = reader.ReadByte();
                if (chunks[i].SectorCount == 0 && chunks[i].Offset == 0) chunks[i].status = ChunkStatus.NotCreated;
                else if (chunks[i].SectorCount == 0) chunks[i].status = ChunkStatus.ZeroLength;
                else if (chunks[i].Offset < 2 && chunks[i].Offset != 0) chunks[i].status = ChunkStatus.InHeader;
                else if (SECTOR_LENGTH * chunks[i].Offset + 5 > fileSize) chunks[i].status = ChunkStatus.OutOfFile;
                else chunks[i].status = ChunkStatus.Ok;
                stream.Seek(i + SECTOR_LENGTH, SeekOrigin.Begin);
                chunks[i].Timestamp = reader.ReadUInt32();
            }
        }

        private void ReadChunks(Stream stream) {
            var reader = new BinaryReader2(stream);
            for (int i = 0; i < 32 * 32; i++) {
                if (chunks[i].status == ChunkStatus.Ok) {
                    stream.Seek(chunks[i].Offset * SECTOR_LENGTH, SeekOrigin.Begin);
                    uint length = reader.ReadUInt32();

                    if (length > stream.Length - stream.Position || length == 0) {
                        chunks[i].status = ChunkStatus.MismatchedLengths;
                        continue;
                    }

                    chunks[i].Length = length;
                    chunks[i].Compression = (CompressionType)reader.ReadByte();

                    if (chunks[i].Offset != 0 && chunks[i].Length != 0) {
                        try {
                            byte[] data = DecompressChunk(reader.ReadBytes((int)chunks[i].Length), chunks[i].Compression);
                            try {
                                chunks[i].Value = NBTFile.FromBytes(data);
                            }
                            catch (Exception ex) {
                                Console.Error.WriteLine($"NBT Parsing failed for chunk {i}: {ex.Message}");
                            }
                        }
                        catch (Exception ex) {
                            Console.Error.WriteLine($"Decompression failed for chunk {i}: {ex.Message}");
                            chunks[i].status = ChunkStatus.MismatchedLengths;
                        }
                    }
                }
            }
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

        public NBTFile? this[byte x, byte z] {
            get => this[x * 32 + z];
            set => this[x * 32 + z] = value;
        }

        public NBTFile? this[int i] {
            get => chunks[i].Value;
            set => chunks[i].Value = value;
        }

        public static void ReplaceChunk(string filePath, byte x, byte z, NBTFile nbt) {
            var region = new RegionFile();
            using FileStream stream = File.Open(filePath, FileMode.OpenOrCreate);

            var reader = new BinaryReader2(stream);
            var writer = new BinaryWriter2(stream);
            region.ReadHeader(stream);

            int chunkIndex = x * 32 + z;

            byte[] nbtBytes = nbt.ToBytes();
            byte[] data = [
                (byte)nbtBytes.Length,
                    (byte)(nbtBytes.Length >> 8),
                    (byte)(nbtBytes.Length >> 16),
                    (byte)CompressionType.GZipCompressed,
                    .. nbtBytes,
                ];
            byte[] paddedData = new byte[(int)Math.Ceiling((float)(data.Length) / SECTOR_LENGTH) * SECTOR_LENGTH];
            Array.Copy(data, paddedData, data.Length);

            byte newSectorCount = (byte)((data.Length + 4) / SECTOR_LENGTH);
            uint newOffset = region.AllocateSpace(stream, newSectorCount);
            
            stream.SetLength(Math.Max(stream.Length, newOffset + data.Length));
            stream.Seek(newOffset, SeekOrigin.Begin);
            writer.Write(data);
            writer.Flush();

            stream.Seek(chunkIndex * 4, SeekOrigin.Begin);
            writer.WriteUInt24(newOffset);
            writer.Write(newSectorCount);
            stream.Seek(chunkIndex * 4 + SECTOR_LENGTH, SeekOrigin.Begin);
            writer.Write((uint)DateTime.UtcNow.Ticks);
            writer.Flush();
        }

        private uint AllocateSpace(FileStream stream, byte newSectorCount) {
            long currentFileSize = stream.Length;
            bool[] freeSectors = new bool[(int)Math.Ceiling((double)currentFileSize / SECTOR_LENGTH)];

            for (int i = 2; i < freeSectors.Length; i++) freeSectors[i] = true;

            for (int i = 0; i < 32 * 32; i++) {
                if (chunks[i].status == ChunkStatus.Ok) {
                    for (int j = 0; j < chunks[i].SectorCount; j++) {
                        freeSectors[chunks[i].Offset + j] = false;
                    }
                }
            }

            int contiguousFree = 0;
            uint freeStart = 0;
            for (int i = 0; i < freeSectors.Length; i++) {
                if (freeSectors[i]) {
                    contiguousFree++;
                    if (contiguousFree == newSectorCount) {
                        freeStart = (uint)(i - newSectorCount + 1) * SECTOR_LENGTH;
                        break;
                    }
                }
                else {
                    contiguousFree = 0;
                }
            }

            if (contiguousFree < newSectorCount) {
                return (uint)currentFileSize;
            }

            return freeStart;
        }

        public List<byte[]> GetChunksCoords() {
            List<byte[]> coords = [];
            for (byte x = 0; x < 0x20; x++) {
                for (byte z = 0; z < 0x20; z++) {
                    if (chunks[x * 32 + z].IsCreated) {
                        coords.Add([x, z]);
                    }
                }
            }
            return coords;
        }

        public List<NBTFile> GetChunks() {
            List<NBTFile> chunks = [];
            foreach (byte[] chunkCoords in GetChunksCoords()) {
                NBTFile? chunk = this[chunkCoords[0], chunkCoords[1]];
                if (chunk != null) chunks.Add(chunk);
            }
            return chunks;
        }
    }
}
