namespace CWV;
internal class Program {
    public static void Main() {
        string fp = "D:\\bigtest.nbt";
        string fp2 = "D:\\bigtest but not really.nbt";
        /*
        using (FileStream fileStream = new FileStream(fp, FileMode.Open)) {
            using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress)) {
                int position = 0;
                using (BinaryReader reader = new BinaryReader(gzipStream, Encoding.UTF8)) {
                    byte[] buffer = new byte[16];
                    int bytesRead;

                    while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0) {
                        Console.Write("{0:x4}: ", position);
                        position += bytesRead;

                        for (var i = 0; i < 16; i++) {
                            if (i < bytesRead)
                                Console.Write("{0:x2} ", (byte)buffer[i]);
                            else
                                Console.Write("   ");
                            if (i == 7) Console.Write("-- ");

                            if (buffer[i] < 0x20 || buffer[i] > 0x7F) buffer[i] = (byte)'.';
                        }
                        var bufferContents = Encoding.UTF8.GetString(buffer);
                        Console.WriteLine("   {0}", bufferContents.Substring(0, bytesRead));
                    }
                }
            }
        }
        */
        Nbt.NBTFile nbt = Nbt.NBTFile.FromFile(fp);
        Console.Write(nbt.PrettyTree());
        nbt.ToFile(fp2, compression: Nbt.CompressionType.Uncompressed);
        nbt = Nbt.NBTFile.FromFile(fp2);
    }
}
