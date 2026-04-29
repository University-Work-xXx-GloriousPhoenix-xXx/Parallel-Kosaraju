using ParallelKosaraju.Algorithm;
using System.Diagnostics;

namespace ParallelKosaraju;

public static class GraphHelper
{
    private static readonly string ResultDir = "F:\\Programmes\\Github\\Reps\\Parallel Kosaraju\\ParallelKosaraju\\ParallelKosaraju\\Result\\";
    private static readonly string DecoPath = Path.Combine(ResultDir, "Deco.txt");
    private static readonly string PurePath = Path.Combine(ResultDir, "Pure.csv");

    private static readonly int MEASURE_RUNS = 5;
    private static readonly int WARMUP_RUNS = 2;

    private static readonly int START_POW = 4;
    private static readonly int END_POW = 7;
    private static readonly int POW_COUNT = END_POW - START_POW + 1;

    static DirectedGraph<int> GenerateRandomGraph(int vertices, int edgeCount, int seed = 42)
    {
        var rand = new Random(seed);
        var g = new DirectedGraph<int>(vertices);
        for (int i = 0; i < edgeCount; i++)
        {
            int from = rand.Next(vertices);
            int to = rand.Next(vertices);
            if (from != to)
                g.AddEdge(from, to);
        }
        return g;
    }
    public static void Benchmark(double edgeRatio)
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

        File.WriteAllText(DecoPath, string.Empty);
        File.WriteAllText(PurePath, string.Empty);

        var separator = "|----------|-------------|-----------------|---------------|-------------|----------|";
        var cmdPattern = "| {0,8} | {1,11} | {2,15:F2} | {3,13:F2} | {4,11:F2} | {5,8} |";
        var csvPattern = "{0};{1};{2};{3};{4};{5}";
        var header = string.Format(cmdPattern,
            "Vertices", "Edges   ",
            "Sequential (ms)", "Parallel (ms)",
            "Speedup (x)", "SCCs  ");

        Console.WriteLine($"{separator}\n{header}\n{separator}");
        File.AppendAllLines(DecoPath, [ separator, header, separator ]);
        File.AppendAllLines(PurePath, [ "v;e;seq;par;acc;q" ]);

        var finder = new SCCFinder<int>();

        foreach (var n in sizes)
        {
            var edgeCount = (int)(n * edgeRatio);
            var graph = GenerateRandomGraph(n, edgeCount);

            for (var wr = 0; wr < WARMUP_RUNS; wr++)
            {
                finder.KosarajuSequential(graph);
                finder.KosarajuParallel(graph);
            }

            long mSeqTotal = 0L, mParTotal = 0L;
            long mSeqQ, mParQ = 0L;
            bool isEq = true;

            for (var r = 0; r < MEASURE_RUNS; r++)
            {
                var mSeqStart = NanoTime();
                var mSeqRes = finder.KosarajuSequential(graph);
                var mSeqEnd = NanoTime();

                var mParStart = NanoTime();
                var mParRes = finder.KosarajuParallel(graph);
                var mParEnd = NanoTime();

                mSeqQ = mSeqRes.Count;
                mParQ = mParRes.Count;
                Console.WriteLine($"{mSeqQ} {mParQ}");

                isEq &= (mSeqQ == mParQ);

                mSeqTotal += (mSeqEnd - mSeqStart);
                mParTotal += (mParEnd - mParStart);
            }

            var mSeqAvg = mSeqTotal / (double)MEASURE_RUNS / 1_000_000.0;
            var mParAvg = mParTotal / (double)MEASURE_RUNS / 1_000_000.0;

            var mPar_mSeq_acc = mSeqAvg / mParAvg;

            var cmdOutput = string.Format(cmdPattern,
                n, edgeCount,
                mSeqAvg, mParAvg,
                mPar_mSeq_acc,
                mParQ);
            var csvOutput = string.Format(csvPattern,
                n, edgeCount,
                mSeqAvg, mParAvg,
                mPar_mSeq_acc,
                mParQ);

            Console.WriteLine(cmdOutput);
            File.AppendAllLines(DecoPath, [cmdOutput]);
            File.AppendAllLines(PurePath, [csvOutput]);
        }

        Console.WriteLine(separator);
        File.AppendAllLines(DecoPath, [separator]);
    }
    private static long NanoTime()
    {
        long nano = 10000L * Stopwatch.GetTimestamp();
        nano /= TimeSpan.TicksPerMillisecond;
        nano *= 100L;
        return nano;
    }
}
