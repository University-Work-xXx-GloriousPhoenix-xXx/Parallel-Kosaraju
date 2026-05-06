using ParallelKosaraju;
using System.Globalization;

namespace ParallelKosaraju;

public enum BenchmarkMode
{
    Classic = 1,
    SequentialOnSizes,
    CompareOnThreads
}

public static class Program
{
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
        ShowMenu();

        var mode = ReadMode();
        if (mode == null)
        {
            Console.WriteLine("Invalid option. Program will exit.");
            return;
        }

        RunMode(mode.Value);
    }

    private static void ShowMenu()
    {
        Console.WriteLine("Select benchmark mode:");
        Console.WriteLine("1. Classic");
        Console.WriteLine("2. SequentialOnSizes");
        Console.WriteLine("3. ParallelOnSizes");
        Console.WriteLine("4. CompareOnThreads");
        Console.Write("Enter option: ");
    }

    private static BenchmarkMode? ReadMode()
    {
        var input = Console.ReadLine();

        if (int.TryParse(input, out int value) &&
            Enum.IsDefined(typeof(BenchmarkMode), value))
        {
            return (BenchmarkMode)value;
        }

        return null;
    }

    private static void RunMode(BenchmarkMode mode)
    {
        switch (mode)
        {
            case BenchmarkMode.Classic:
                RunClassic();
                break;

            case BenchmarkMode.SequentialOnSizes:
                RunSequentialOnSizes();
                break;

            case BenchmarkMode.CompareOnThreads:
                RunCompareOnThreads();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private static void RunClassic()
    {
        var ratio = ReadEdgeRatio();
        var suffix = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        GraphHelper.Benchmark(ratio, suffix);
    }

    private static void RunSequentialOnSizes()
    {
        var ratio = ReadEdgeRatio();
        GraphHelper.BenchmarkSequentialOnSizes(ratio);
    }

    private static void RunCompareOnThreads()
    {
        var ratio = ReadEdgeRatio();

        foreach (var size in sizes)
        {
            GraphHelper.BenchmarkParallelOnThreads(size, ratio);
        }
    }

    private static double ReadEdgeRatio()
    {
        Console.Write("Enter the edge ratio to vertices: ");

        if (double.TryParse(Console.ReadLine(), CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        Console.WriteLine("Invalid input. Using default value: 5.0");
        return edgeRatio;
    }
}