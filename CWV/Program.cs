using static CWV.Region;
using System.Text.RegularExpressions;
using System;

namespace CWV;

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
    private static partial Regex DimFolderRegex();

    static List<string> GetRegionPaths() {
        List<string> regions = [];
        foreach (string world in ListFolders(GetPathToSaves())) {
            Console.WriteLine($"reading {world}");
            var dimensions = from d in new DirectoryInfo(world).GetDirectories("*", SearchOption.AllDirectories)
                             where DimFolderRegex().IsMatch(d.Name)
                             select d.FullName;
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

    public static void Main(string[] args) {
        Console.WriteLine("welcome to see double you vee not working edition");
        Console.WriteLine("i am too lazy to make arguments");
        Console.WriteLine("короче кирилл потом сделай в интерфейсе чтобы были следуйщие параметры");
        Console.WriteLine(" > подробный режим чтобы писало как рандомизирует каждый чанк");
        Console.WriteLine(" > возможность выбора своих папок типо чтобы добавляло в нижнем цикле");
        Console.WriteLine(" > разные режимы я в итоге понял как оптимизировать CWV чтобы он суко не жрал 8 гб!!1!!1111");
        Console.WriteLine("сделаю всё сам но ты сделай типо чтобы можно было вот это выбирать");
        List<Chunk> chunks = [];
        Dictionary<string, List<byte[]>> availableChunks = [];
        foreach (string regionPath in GetRegionPaths()) {
            Console.WriteLine($"reading {regionPath}");
            availableChunks[regionPath] = [];
            var region = RegionFile.FromFile(regionPath);
            foreach (byte[] xz in region.GetChunksCoords()) {
                chunks.Add(region[xz[0], xz[1]]); // ураааааааа потребление памяти 3535098 йоттабайт
                availableChunks[regionPath].Add(xz);
            }
        }
        /* не рабочий псевдокод (нужно будет написать эти методы)
        while (chunks.Count > 0) {
            print($"{chunks.Count} chunks left to randomize...");
        }*/
    }
}
