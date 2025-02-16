﻿using static CWV.Region;
using static CWV.Nbt;
using System.Text.RegularExpressions;

namespace CWV;

public static class SomeExtensions {
    private static readonly Random random = new();

    public static T Choice<T>(this IEnumerable<T> enumerable) {
        return enumerable.ElementAt(random.Next(enumerable.Count()));
    }

    public static T PopRandom<T>(this List<T> list) {
        int index = random.Next(list.Count);
        T t = list[index];
        list.RemoveAt(index);
        return t;
    }
}

internal partial class Program {
    static string GetPathToSaves() {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ".minecraft", "saves"
        );
    }

    static IEnumerable<string> ListFolders(string path) {
        return from i in Directory.EnumerateDirectories(path) select Path.Combine(path, i);
    }

    static IEnumerable<string> ListFiles(string path) {
        return from i in Directory.EnumerateFiles(path) select Path.Combine(path, i);
    }



    [GeneratedRegex(@"^(region|DIM-?\d+)$")]
    private static partial Regex RegionDirRegex();

    static List<string> GetRegionPaths() {
        List<string> regions = [];
        foreach (string world in ListFolders(GetPathToSaves())) {
            Console.WriteLine($"reading {world}");
            var dimensions = ListFolders(world).Where(d => RegionDirRegex().IsMatch(d));
            foreach (string dimension in dimensions) {
                Console.WriteLine($"    reading {dimension}");
                if (dimension == "region") {
                    foreach (string region in ListFiles(dimension)) {
                        Console.WriteLine($"        adding {region}");
                        regions.Add(region);
                    }
                }
                else {
                    string regionFolder = Path.Combine(dimension, "region");
                    if (Path.Exists(regionFolder)) {
                        foreach (string region in ListFiles(regionFolder)) {
                            Console.WriteLine($"        adding {region}");
                            regions.Add(region);
                        }
                    }
                }
            }
        }
        return regions;
    }

    public static void Main() {
        Console.WriteLine("welcome to see double you vee not working edition");
        Console.WriteLine("i am too lazy to make arguments");
        Console.WriteLine("короче кирилл потом сделай в интерфейсе чтобы были следуйщие параметры");
        Console.WriteLine(" > подробный режим чтобы писало как рандомизирует каждый чанк");
        Console.WriteLine(" > возможность выбора своих папок типо чтобы добавляло в нижнем цикле");
        Console.WriteLine(" > разные режимы я в итоге понял как оптимизировать CWV чтобы он суко не жрал 8 гб!!1!!1111");
        Console.WriteLine("сделаю всё сам но ты сделай типо чтобы можно было вот это выбирать");
        Dictionary<string, List<byte[]>> availableChunks = [];
        foreach (string regionPath in GetRegionPaths()) {
            Console.WriteLine($"reading {regionPath}");
            availableChunks[regionPath] = [];
            RegionFile region;
            try {
                region = RegionFile.FromFile(regionPath);
                foreach (byte[] xz in region.GetChunksCoords()) {
                    NBTFile? chunk = region[xz[0], xz[1]];
                    if (chunk != null) {
                        availableChunks[regionPath].Add(xz);
                    }
                }

                if (availableChunks[regionPath].Count == 0) availableChunks.Remove(regionPath);
            }
            catch (IOException ex) {
                Console.Error.WriteLine($"Failed to read file: {ex.Message}");
            }
        }

        string firstPath = availableChunks.Keys.Choice();
        byte[] firstCoords = availableChunks[firstPath].PopRandom();
        NBTFile? nbt = RegionFile.GetChunk(firstPath, firstCoords[0], firstCoords[1]);

        try {
            while (availableChunks.Keys.Count > 0) {
                Console.WriteLine("possibly useful print statements");
                string randomPath = availableChunks.Keys.Choice(); // --->
                byte[] b = availableChunks[randomPath].PopRandom(); // --->

                nbt = RegionFile.ReplaceAndPopChunk(randomPath, b[0], b[1], nbt);

                if (availableChunks[randomPath].Count == 0) availableChunks.Remove(randomPath);
            }
            RegionFile.ReplaceChunk(firstPath, firstCoords[0], firstCoords[1], nbt);
            Console.WriteLine("this will either work or dont");
        }
        catch (IOException ex) {
            Console.Error.WriteLine($"uhhhh sorry but with current realisation this thing just died: {ex.Message}");
        }
    }
}
