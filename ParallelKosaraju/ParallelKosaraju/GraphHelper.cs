using System.Diagnostics;

namespace ParallelKosaraju;

public static class GraphHelper
{
    private static readonly string ResultPath = "F:\\Programmes\\Github\\Reps\\Parallel Kosaraju\\ParallelKosaraju\\ParallelKosaraju\\result\\result.csv";
    private static readonly string OutputPath = "F:\\Programmes\\Github\\Reps\\Parallel Kosaraju\\ParallelKosaraju\\ParallelKosaraju\\result\\output.txt";
    private static readonly int MEASURE_RUNS = 15;
    private static readonly int WARMUP_RUNS = 5;
    private static readonly int DEGREE = 5;
    private static readonly int START_POW = 3;
    private static readonly int POW_COUNT = 4;

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
    public static void Benchmark()
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

        var header = "| Vertices |   Edges   | Sequential (ms) | Parallel (ms) | Acceleration |";
        var separator = "|----------|-----------|-----------------|---------------|--------------|";

        Console.WriteLine($"{separator}\n{header}\n{separator}");
        File.AppendAllLines(OutputPath, [ separator, header, separator ]);
        File.AppendAllLines(ResultPath, [ "v;e;seq;par;acc" ]);

        foreach (var n in sizes)
        {
            for (var wr = 0; wr < WARMUP_RUNS; wr++)
            {
                var warmup_graph = GenerateRandomGraph(n, DEGREE);
                SCCFinder.KosarajuSequential(warmup_graph);
                SCCFinder.KosarajuParallel(warmup_graph);
            }

            var graph = GenerateRandomGraph(n, DEGREE);
            var seqTotal = 0L;
            var parTotal = 0L;

            for (var r = 0; r < MEASURE_RUNS; r++)
            {
                var seqStart = NanoTime();
                SCCFinder.KosarajuSequential(graph);
                var seqEnd = NanoTime();
                seqTotal += (seqEnd - seqStart);

                var parStart = NanoTime();
                SCCFinder.KosarajuParallel(graph);
                var parEnd = NanoTime();
                parTotal += (parEnd - parStart);
            }

            var seqAvg = seqTotal / (double)MEASURE_RUNS / 1_000_000.0;
            var parAvg = parTotal / (double)MEASURE_RUNS / 1_000_000.0;
            var speedup = seqAvg / parAvg;

            var outputLine = $"| {n,8} | {n * DEGREE,9} | {seqAvg,15:F2} | {parAvg,13:F2} | {speedup,11:F2}x |";
            var resultLine = $"{n};{n * DEGREE};{seqAvg:F2};{parAvg:F2};{speedup:F2}";


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
