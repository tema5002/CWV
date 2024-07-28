namespace CWV;
internal class Program {
    public static void Main() {
        string fp = "D:\\bigtest.nbt";
        string fp2 = "D:\\bigtest but not really.nbt";
        Nbt.NBTFile nbt = Nbt.NBTFile.FromFile(fp);
        Console.Write(nbt.PrettyTree());
        nbt.ToFile(fp2, compression: Nbt.CompressionType.Uncompressed);
        nbt = Nbt.NBTFile.FromFile(fp2);
    }
}
