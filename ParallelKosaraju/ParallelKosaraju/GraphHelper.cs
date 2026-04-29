using ParallelKosaraju.Algorithm;
using System.Diagnostics;

namespace ParallelKosaraju;

public static class GraphHelper
{
    private static readonly string DecoPath = "F:\\Programmes\\Github\\Reps\\Parallel Kosaraju\\ParallelKosaraju\\ParallelKosaraju\\Result\\Deco.txt";
    private static readonly string PurePath = "F:\\Programmes\\Github\\Reps\\Parallel Kosaraju\\ParallelKosaraju\\ParallelKosaraju\\Result\\Pure.csv";
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
        
        File.WriteAllText(PurePath, string.Empty);
        File.WriteAllText(DecoPath, string.Empty);

        var separator = "|----------|-------------|----------------|--------------|--------------|-------------|-------------|-------------|---------------|-------------|-------------|----------|";
        var cmdPattern = "| {0,8} | {1,11} | {2,12:F2} | {3,12:F2} | {4,11:F2} | {5,11} | {6,11} | {7,8} |";
        var csvPattern = "{0};{1};{3:F2.3};{4:F2.3};{7:F2.2};{9};{10};{11}";
        var header = string.Format(cmdPattern,
            "Vertices", "Edges",
            "Mod Seq (ms)", "Mod Par (ms)",
            "MP ~ MS Acc",
            "Mod Seq (q)", "Mod Par (q)",
            "Is Equal");

        Console.WriteLine($"{separator}\n{header}\n{separator}");
        File.AppendAllLines(PurePath, [ separator, header, separator ]);
        File.AppendAllLines(DecoPath, [ "v;e;t_seq;t_par;acc;q_seq;q_par;eq" ]);

        var finder = new SCCFinder<int>();

        foreach (var n in sizes)
        {
            var edgeCount = (int)(n * edgeRatio);
            var graph = GenerateRandomGraph(n, edgeCount);

            for (var wr = 0; wr < WARMUP_RUNS; wr++)
            {
                finder.ModifiedKosarajuSequential(graph);
                finder.ModifiedKosarajuParallel(graph);
            }

            long mSeqTotal = 0L, mParTotal = 0L;
            long mSeqQ = 0L, mParQ = 0L;
            bool isEq = true;

            for (var r = 0; r < MEASURE_RUNS; r++)
            {
                var mSeqStart = NanoTime();
                var mSeqRes = finder.ModifiedKosarajuSequential(graph);
                var mSeqEnd = NanoTime();

                var mParStart = NanoTime();
                var mParRes = finder.ModifiedKosarajuParallel(graph);
                var mParEnd = NanoTime();

                mSeqQ = mSeqRes.Count;
                mParQ = mParRes.Count;

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
                mSeqQ, mParQ,
                isEq);
            var csvOutput = string.Format(csvPattern,
                n, edgeCount,
                mSeqAvg, mParAvg,
                mPar_mSeq_acc,
                mSeqQ, mParQ,
                isEq);

            Console.WriteLine(cmdOutput);
            File.AppendAllLines(PurePath, [cmdOutput]);
            File.AppendAllLines(DecoPath, [csvOutput]);
        }

        Console.WriteLine(separator);
        File.AppendAllLines(PurePath, [separator]);
    }
    private static long NanoTime()
    {
        long nano = 10000L * Stopwatch.GetTimestamp();
        nano /= TimeSpan.TicksPerMillisecond;
        nano *= 100L;
        return nano;
    }
}
