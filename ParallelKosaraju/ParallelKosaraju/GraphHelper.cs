using ParallelKosaraju.Algorithm;
using System.Diagnostics;

namespace ParallelKosaraju;

public static class GraphHelper
{
    private static readonly string ResultPath = "F:\\Programmes\\Github\\Reps\\Parallel Kosaraju\\ParallelKosaraju\\ParallelKosaraju\\result\\result.csv";
    private static readonly string OutputPath = "F:\\Programmes\\Github\\Reps\\Parallel Kosaraju\\ParallelKosaraju\\ParallelKosaraju\\result\\output.txt";
    private static readonly int MEASURE_RUNS = 15;
    private static readonly int WARMUP_RUNS = 5;
    private static readonly int START_POW = 4;
    private static readonly int END_POW = 7;
    private static readonly int POW_COUNT = END_POW - START_POW + 1;

    static DirectedGraph<int> GenerateRandomGraph(int vertices, int degree, int seed = 42)
    {
        var rand = new Random(seed);
        var g = new DirectedGraph<int>(vertices);
        var edges = vertices * degree;
        for (int i = 0; i < edges; i++)
        {
            int from = rand.Next(vertices);
            int to = rand.Next(vertices);
            if (from != to)
                g.AddEdge(from, to);
        }
        return g;
    }
    public static void Benchmark(int degree)
    {
        var sizes = new List<int>();

        var factor = (int)Math.Pow(10, START_POW);
        for (var pow = 0; pow < POW_COUNT; pow++)
        {
            sizes.Add(factor);
            sizes.Add(2 * factor);
            sizes.Add(5 * factor);
            factor *= 10;
        }
        
        File.WriteAllText(OutputPath, string.Empty);
        File.WriteAllText(ResultPath, string.Empty);

        var degreeLength = degree.ToString().Length;
        var degreeLine = $"| {degree}x |";
        var degreeTopper = $"|{new string('-', degreeLength + 3)}|";
        var header = "| Vertices |    Edges    | Sequential (ms) | Parallel (ms) | Acceleration | Sequential (q) | Parallel (q) |";
        var separator = "|----------|-------------|-----------------|---------------|--------------|----------------|--------------|";

        Console.WriteLine($"{degreeTopper}\n{degreeLine}\n{separator}\n{header}\n{separator}");
        File.AppendAllLines(OutputPath, [ degreeTopper, degreeLine, separator, header, separator ]);
        File.AppendAllLines(ResultPath, [ "v;e;seq;par;acc" ]);

        var finder = new SCCFinder<int>();

        foreach (var n in sizes)
        {
            for (var wr = 0; wr < WARMUP_RUNS; wr++)
            {
                var warmup_graph = GenerateRandomGraph(n, degree);
                finder.KosarajuSequential(warmup_graph);
                finder.KosarajuParallel(warmup_graph);
            }

            var graph = GenerateRandomGraph(n, degree);
            var seqTotal = 0L;
            var parTotal = 0L;

            var seqQ = 0;
            var parQ = 0;

            for (var r = 0; r < MEASURE_RUNS; r++)
            {
                var seqStart = NanoTime();
                var seqRes = finder.KosarajuSequential(graph);
                var seqEnd = NanoTime();
                seqQ = seqRes.Count;
                seqTotal += (seqEnd - seqStart);

                var parStart = NanoTime();
                var parRes = finder.KosarajuParallel(graph);
                var parEnd = NanoTime();
                parQ = parRes.Count;
                parTotal += (parEnd - parStart);
            }

            var seqAvg = seqTotal / (double)MEASURE_RUNS / 1_000_000.0;
            var parAvg = parTotal / (double)MEASURE_RUNS / 1_000_000.0;
            var speedup = seqAvg / parAvg;

            var outputLine = $"| {n,8} | {n * degree,11} | {seqAvg,15:F2} | {parAvg,13:F2} | {speedup,11:F2}x | {seqQ,14} | {parQ,12} |";
            var resultLine = $"{n};{n * degree};{seqAvg:F2};{parAvg:F2};{speedup:F2}";


            Console.WriteLine(outputLine);
            File.AppendAllLines(OutputPath, [outputLine]);
            File.AppendAllLines(ResultPath, [resultLine]);
        }

        Console.WriteLine(separator);
        File.AppendAllLines(OutputPath, [separator]);
    }
    private static long NanoTime()
    {
        long nano = 10000L * Stopwatch.GetTimestamp();
        nano /= TimeSpan.TicksPerMillisecond;
        nano *= 100L;
        return nano;
    }
}
