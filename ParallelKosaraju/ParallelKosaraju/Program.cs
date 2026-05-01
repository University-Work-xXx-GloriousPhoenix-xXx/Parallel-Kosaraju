using ParallelKosaraju;
using System.Globalization;

namespace ParallelKosaraju;

public enum BenchmarkMode
{
    Classic,
    SequentialOnSizes,
    ParallelOnSizes,
    CompareOnThreads
}

public static class Program
{
    private static readonly int globalRuns = 10;
    private static double edgeRatio = 5d;
    private static readonly List<int> sizes =
    [
        1_000_000,
        2_000_000,
        5_000_000,
        10_000_000,
        20_000_000
    ];

    public static void Main(string[] args)
    {
        var mode = BenchmarkMode.CompareOnThreads;
        var parsedRatio = edgeRatio;
        switch (mode)
        {
            case BenchmarkMode.Classic:
                Console.Write("Enter the edge ratio to vertices: ");
                if (double.TryParse(Console.ReadLine(), CultureInfo.InvariantCulture, out parsedRatio))
                {
                    edgeRatio = parsedRatio;
                }
                for (var gr = 0; gr < globalRuns; gr++)
                {
                    var suffix = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
                    GraphHelper.Benchmark(edgeRatio, suffix);
                    Thread.Sleep(5);
                }
            break;
            case BenchmarkMode.SequentialOnSizes:
                Console.Write("Enter the edge ratio to vertices: ");
                if (double.TryParse(Console.ReadLine(), CultureInfo.InvariantCulture, out parsedRatio))
                {
                    edgeRatio = parsedRatio;
                }
                GraphHelper.BenchmarkSequentialOnSizes(edgeRatio);
            break;
            case BenchmarkMode.ParallelOnSizes:
                Console.Write("Enter the edge ratio to vertices: ");
                if (double.TryParse(Console.ReadLine(), CultureInfo.InvariantCulture, out parsedRatio))
                {
                    edgeRatio = parsedRatio;
                }
                GraphHelper.BenchmarkParallelOnSizes(parsedRatio);
            break;
            case BenchmarkMode.CompareOnThreads:
                Console.Write("Enter the edge ratio to vertices: ");
                if (double.TryParse(Console.ReadLine(), CultureInfo.InvariantCulture, out parsedRatio))
                {
                    edgeRatio = parsedRatio;
                }
                foreach (var size in sizes)
                {
                    GraphHelper.BenchmarkParallelOnThreads(size, edgeRatio);
                }
            break;
            default:
                throw new ArgumentException("Incorrect option");
        }
    }
}