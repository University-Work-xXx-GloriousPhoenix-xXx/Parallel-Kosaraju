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
        var separator = "|----------|-------------|----------------|--------------|--------------|-------------|-------------|-------------|---------------|-------------|-------------|----------|";
        var cmdPattern = "| {0,8} | {1,11} | {2,14:F2} | {3,12:F2} | {4,12:F2} | {5,11:F2} | {6,11:F2} | {7,11:F2} | {8,13} | {9,11} | {10,11} | {11,8} |";
        var csvPattern = "{0};{1};{2:F2};{3:F2.3};{4:F2.3};{5:F2.2};{6:F2.2};{7:F2.2};{8};{9};{10};{11}";
        var header = string.Format(cmdPattern,
            "Vertices", "Edges",
            "Basic Seq (ms)", "Mod Seq (ms)", "Mod Par (ms)",
            "MS ~ BS Acc", "MP ~ BS Acc", "MP ~ MS Acc",
            "Basic Seq (q)", "Mod Seq (q)", "Mod Par (q)",
            "Is Equal");

        Console.WriteLine($"{degreeTopper}\n{degreeLine}\n{separator}\n{header}\n{separator}");
        File.AppendAllLines(OutputPath, [ degreeTopper, degreeLine, separator, header, separator ]);
        File.AppendAllLines(ResultPath, [ "v;e;t_bseq;t_mseq;t_mpar;acc_mseq_bseq;acc_mpar_bseq;acc_mpar_mseq;q_bseq;q_mseq;q_mpar;eq" ]);

        var finder = new SCCFinder<int>();

        foreach (var n in sizes)
        {
            for (var wr = 0; wr < WARMUP_RUNS; wr++)
            {
                var warmup_graph = GenerateRandomGraph(n, degree);
                finder.BasicKosarajuSequential(warmup_graph);
                finder.ModifiedKosarajuSequential(warmup_graph);
                finder.ModifiedKosarajuParallel(warmup_graph);
            }

            var graph = GenerateRandomGraph(n, degree);
            long bSeqTotal = 0L, mSeqTotal = 0L, mParTotal = 0L;
            long bSeqQ = 0L, mSeqQ = 0L, mParQ = 0L;
            bool isEq = true;

            for (var r = 0; r < MEASURE_RUNS; r++)
            {
                var bSeqStart = NanoTime();
                var bSeqRes = finder.BasicKosarajuSequential(graph);
                var bSeqEnd = NanoTime();

                var mSeqStart = NanoTime();
                var mSeqRes = finder.ModifiedKosarajuSequential(graph);
                var mSeqEnd = NanoTime();

                var mParStart = NanoTime();
                var mParRes = finder.ModifiedKosarajuParallel(graph);
                var mParEnd = NanoTime();

                bSeqQ = bSeqRes.Count;
                mSeqQ = mSeqRes.Count;
                mParQ = mParRes.Count;

                isEq &= (bSeqQ == mSeqQ);
                isEq &= (bSeqQ == mParQ);
                isEq &= (mSeqQ == mParQ);

                bSeqTotal += (bSeqEnd - bSeqStart);
                mSeqTotal += (mSeqEnd - mSeqStart);
                mParTotal += (mParEnd - mParStart);
            }

            var bSeqAvg = bSeqTotal / (double)MEASURE_RUNS / 1_000_000.0;
            var mSeqAvg = mSeqTotal / (double)MEASURE_RUNS / 1_000_000.0;
            var mParAvg = mParTotal / (double)MEASURE_RUNS / 1_000_000.0;

            var mSeq_bSeq_acc = bSeqAvg / mSeqAvg;
            var mPar_bSeq_acc = bSeqAvg / mParAvg;
            var mPar_mSeq_acc = mSeqAvg / mParAvg;

            var edgeCount = n * degree;
            
            var cmdOutput = string.Format(cmdPattern,
                n, edgeCount,
                bSeqAvg, mSeqAvg, mParAvg,
                mSeq_bSeq_acc, mPar_bSeq_acc, mPar_mSeq_acc,
                bSeqQ, mSeqQ, mParQ,
                isEq);
            var csvOutput = string.Format(csvPattern,
                n, edgeCount,
                bSeqAvg, mSeqAvg, mParAvg,
                mSeq_bSeq_acc, mPar_bSeq_acc, mPar_mSeq_acc,
                bSeqQ, mSeqQ, mParQ,
                isEq);

            Console.WriteLine(cmdOutput);
            File.AppendAllLines(OutputPath, [cmdOutput]);
            File.AppendAllLines(ResultPath, [csvOutput]);
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
