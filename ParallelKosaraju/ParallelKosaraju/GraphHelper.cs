using ParallelKosaraju.Algorithm;
using System.Diagnostics;

namespace ParallelKosaraju;

public static class GraphHelper
{
    private static readonly string ResultDir = "F:\\Programmes\\Github\\Reps\\Parallel Kosaraju\\Result";

    private static readonly int MEASURE_RUNS = 10;
    private static readonly int WARMUP_RUNS = 3;

    private static readonly int START_POW = 4;
    private static readonly int END_POW = 7;
    private static readonly int POW_COUNT = END_POW - START_POW + 1;

    private static readonly SCCFinder<int> finder = new();

    public static DirectedGraph<int> GenerateRandomGraph(int vertices, int edgeCount, int seed = 42)
    {
        var rand = new Random(seed);
        var g = new DirectedGraph<int>(vertices);
        for (var i = 0; i < edgeCount; i++)
        {
            var from = rand.Next(vertices);
            var to = rand.Next(vertices);
            if (from != to)
                g.AddEdge(from, to);
        }
        return g;
    }
    public static void BenchmarkSequentialOnSizes(double edgeRatio)
    {
        var dir = Path.Combine(ResultDir, "SequentialOnSizes\\");
        Directory.CreateDirectory(dir);

        var suffix = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var decoPath = Path.Combine(dir, $"SequentialOnSizes_{suffix}.txt");
        var purePath = Path.Combine(dir, $"SequentialOnSizes_{suffix}.csv");

        File.WriteAllText(decoPath, string.Empty);
        File.WriteAllText(purePath, string.Empty);

        var sizes = new List<int>();
        var factor = (int)Math.Pow(10, START_POW);
        for (var pow = 0; pow < POW_COUNT; pow++)
        {
            sizes.Add(factor);
            sizes.Add(2 * factor);
            sizes.Add(5 * factor);
            factor *= 10;
        }

        var separator = "|------------|-----------|--------------|--------------|";
        var cmdPattern = "| {0,10} | {1,9} | {2,12:F2} | {3,12} |";
        var header = string.Format(cmdPattern, "Vertices", "Edges", "T (ms)", "T(V,E)");

        Console.WriteLine($"{separator}\n{header}\n{separator}");
        File.AppendAllLines(decoPath, [separator, header, separator]);
        File.AppendAllLines(purePath, ["v;e;t_ms;t_calc"]);

        foreach (var size in sizes)
        {
            var edgeCount = (int)(edgeRatio * size);
            var g = GenerateRandomGraph(size, edgeCount);

            var start = NanoTime();
            _ = finder.KosarajuSequential(g);
            var end = NanoTime();

            var tExp = (end - start) / 1_000_000.0;
            var tCal = 3 * size + 2 * edgeCount;

            var cmdOutput = string.Format(cmdPattern, size, edgeCount, tExp, tCal);
            var csvOutput = $"{size};{edgeCount};{tExp};{tCal}";

            Console.WriteLine(cmdOutput);
            File.AppendAllLines(decoPath, [cmdOutput]);
            File.AppendAllLines(purePath, [csvOutput]);
        }

        Console.WriteLine(separator);
        File.AppendAllLines(decoPath, [separator]);
    }

    public static void BenchmarkParallelOnSizes(double edgeRatio, int maxDegreeOfParallelism = -1)
    {
        var dir = Path.Combine(ResultDir, "ParallelOnSizes\\");
        Directory.CreateDirectory(dir);

        var suffix = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var decoPath = Path.Combine(dir, $"ParallelOnSizes_{suffix}.txt");
        var purePath = Path.Combine(dir, $"ParallelOnSizes_{suffix}.csv");

        File.WriteAllText(decoPath, string.Empty);
        File.WriteAllText(purePath, string.Empty);

        var sizes = new List<int>();
        var factor = (int)Math.Pow(10, START_POW);
        for (var pow = 0; pow < POW_COUNT; pow++)
        {
            sizes.Add(factor);
            sizes.Add(2 * factor);
            sizes.Add(5 * factor);
            factor *= 10;
        }

        var separator = "|------------|-----------|--------------|--------------|";
        var cmdPattern = "| {0,10} | {1,9} | {2,12:F2} | {3,12} |";
        var header = string.Format(cmdPattern, "Vertices", "Edges", "T (ms)", "T(V,E)");

        Console.WriteLine($"{separator}\n{header}\n{separator}");
        File.AppendAllLines(decoPath, [separator, header, separator]);
        File.AppendAllLines(purePath, ["v;e;t_ms;t_calc"]);

        foreach (var size in sizes)
        {
            var edgeCount = (int)(edgeRatio * size);
            var g = GenerateRandomGraph(size, edgeCount);

            var start = NanoTime();
            _ = finder.KosarajuParallel(g);
            var end = NanoTime();

            var tExp = (end - start) / 1_000_000.0;
            var tCal = 3 * size + 2 * edgeCount;

            var cmdOutput = string.Format(cmdPattern, size, edgeCount, tExp, tCal);
            var csvOutput = $"{size};{edgeCount};{tExp};{tCal}";

            Console.WriteLine(cmdOutput);
            File.AppendAllLines(decoPath, [cmdOutput]);
            File.AppendAllLines(purePath, [csvOutput]);
        }

        Console.WriteLine(separator);
        File.AppendAllLines(decoPath, [separator]);
    }

    public static void BenchmarkParallelOnThreads(int size, double edgeRatio)
    {
        var dir = Path.Combine(ResultDir, "ParallelOnThreads\\");
        Directory.CreateDirectory(dir);

        var decoPath = Path.Combine(dir, $"ThreadTest_{size}.txt");
        var purePath = Path.Combine(dir, $"ThreadTest_{size}.csv");

        File.WriteAllText(decoPath, string.Empty);
        File.WriteAllText(purePath, string.Empty);

        var threadCounts = new int[] { 1, 2, 4, 6, 8, 10, 12, 14, 16, 20, 24 };

        var genDone = 0;
        var warmSeqDone = 0;
        var warmParDone = 0;
        var measSeqDone = 0;
        var measParDone = 0;
        var warmParTotal = WARMUP_RUNS * threadCounts.Length;
        var measParTotal = MEASURE_RUNS * threadCounts.Length;

        DrawProgress(genDone, warmSeqDone, WARMUP_RUNS, warmParDone, warmParTotal, measSeqDone, MEASURE_RUNS, measParDone, measParTotal);

        var graph = GenerateRandomGraph(size, (int)(size * edgeRatio));
        genDone = 1;
        DrawProgress(genDone, warmSeqDone, WARMUP_RUNS, warmParDone, warmParTotal, measSeqDone, MEASURE_RUNS, measParDone, measParTotal);

        for (var wr = 0; wr < WARMUP_RUNS; wr++)
        {
            finder.KosarajuSequential(graph);
            warmSeqDone++;
            DrawProgress(genDone, warmSeqDone, WARMUP_RUNS, warmParDone, warmParTotal, measSeqDone, MEASURE_RUNS, measParDone, measParTotal);
        }

        var seqTotal = 0L;
        for (var r = 0; r < MEASURE_RUNS; r++)
        {
            var s = NanoTime();
            finder.KosarajuSequential(graph);
            seqTotal += NanoTime() - s;

            measSeqDone++;
            DrawProgress(genDone, warmSeqDone, WARMUP_RUNS, warmParDone, warmParTotal, measSeqDone, MEASURE_RUNS, measParDone, measParTotal);
        }
        var seqAvg = seqTotal / (double)MEASURE_RUNS / 1_000_000.0;

        var separator = "|---------|-----------------|---------------|-------------|";
        var cmdPattern = "| {0,7} | {1,15:F2} | {2,13:F2} | {3,11:F2} |";
        var csvPattern = "{0};{1};{2};{3}";
        var header = string.Format(cmdPattern, "Threads", "Sequential (ms)", "Parallel (ms)", "Speedup (x)");

        Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
        Console.WriteLine($"{separator}\n{header}\n{separator}");
        File.AppendAllLines(decoPath, [separator, header, separator]);
        File.AppendAllLines(purePath, ["threads;seq;par;speedup"]);

        foreach (var threads in threadCounts)
        {
            for (var wr = 0; wr < WARMUP_RUNS; wr++)
            {
                finder.KosarajuParallel(graph, threads);
                warmParDone++;
                DrawProgress(genDone, warmSeqDone, WARMUP_RUNS, warmParDone, warmParTotal, measSeqDone, MEASURE_RUNS, measParDone, measParTotal);
            }

            var parTotal = 0L;
            for (var r = 0; r < MEASURE_RUNS; r++)
            {
                var s = NanoTime();
                finder.KosarajuParallel(graph, threads);
                parTotal += NanoTime() - s;

                measParDone++;
                DrawProgress(genDone, warmSeqDone, WARMUP_RUNS, warmParDone, warmParTotal, measSeqDone, MEASURE_RUNS, measParDone, measParTotal);
            }

            var parAvg = parTotal / (double)MEASURE_RUNS / 1_000_000.0;
            var speedup = seqAvg / parAvg;

            var cmdOutput = string.Format(cmdPattern, threads, seqAvg, parAvg, speedup);
            var csvOutput = string.Format(csvPattern, threads, seqAvg, parAvg, speedup);

            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
            Console.WriteLine(cmdOutput);
            File.AppendAllLines(decoPath, [cmdOutput]);
            File.AppendAllLines(purePath, [csvOutput]);
        }

        Console.WriteLine(separator);
        File.AppendAllLines(decoPath, [separator]);
    }

    public static void Benchmark(double edgeRatio, string suffix)
    {
        var dir = Path.Combine(ResultDir, "Classic\\");
        Directory.CreateDirectory(dir);

        var decoPath = Path.Combine(dir, $"Deco_{suffix}.txt");
        var purePath = Path.Combine(dir, $"Pure_{suffix}.csv");

        var sizes = new List<int>();
        var factor = (int)Math.Pow(10, START_POW);
        for (var pow = 0; pow < POW_COUNT; pow++)
        {
            sizes.Add(factor);
            sizes.Add(2 * factor);
            sizes.Add(5 * factor);
            factor *= 10;
        }

        File.WriteAllText(decoPath, string.Empty);
        File.WriteAllText(purePath, string.Empty);

        var separator = "|----------|-------------|-----------------|---------------|-------------|----------|";
        var cmdPattern = "| {0,8} | {1,11} | {2,15:F2} | {3,13:F2} | {4,11:F2} | {5,8} |";
        var csvPattern = "{0};{1};{2};{3};{4};{5}";
        var header = string.Format(cmdPattern,
            "Vertices", "Edges   ",
            "Sequential (ms)", "Parallel (ms)",
            "Speedup (x)", "SCCs  ");

        Console.WriteLine($"{separator}\n{header}\n{separator}");
        File.AppendAllLines(decoPath, [separator, header, separator]);
        File.AppendAllLines(purePath, ["v;e;seq;par;acc;q"]);

        foreach (var n in sizes)
        {
            var edgeCount = (int)(n * edgeRatio);
            var genDone = 0;
            var warmSeqDone = 0;
            var warmParDone = 0;
            var measSeqDone = 0;
            var measParDone = 0;
            DrawProgress(genDone, warmSeqDone, WARMUP_RUNS, warmParDone, WARMUP_RUNS, measSeqDone, MEASURE_RUNS, measParDone, MEASURE_RUNS);

            var graph = GenerateRandomGraph(n, edgeCount);
            genDone = 1;
            DrawProgress(genDone, warmSeqDone, WARMUP_RUNS, warmParDone, WARMUP_RUNS, measSeqDone, MEASURE_RUNS, measParDone, MEASURE_RUNS);

            for (var wr = 0; wr < WARMUP_RUNS; wr++)
            {
                finder.KosarajuSequential(graph);
                warmSeqDone++;
                DrawProgress(genDone, warmSeqDone, WARMUP_RUNS, warmParDone, WARMUP_RUNS, measSeqDone, MEASURE_RUNS, measParDone, MEASURE_RUNS);
            }

            var seqQ = 0L;
            var seqTotal = 0L;
            for (var r = 0; r < MEASURE_RUNS; r++)
            {
                var seqStart = NanoTime();
                var seqRes = finder.KosarajuSequential(graph);
                var seqEnd = NanoTime();

                measSeqDone++;
                DrawProgress(genDone, warmSeqDone, WARMUP_RUNS, warmParDone, WARMUP_RUNS, measSeqDone, MEASURE_RUNS, measParDone, MEASURE_RUNS);

                seqQ = seqRes.Count;
                seqTotal += (seqEnd - seqStart);
            }

            for (var wr = 0; wr < WARMUP_RUNS; wr++)
            {
                finder.KosarajuParallel(graph);
                warmParDone++;
                DrawProgress(genDone, warmSeqDone, WARMUP_RUNS, warmParDone, WARMUP_RUNS, measSeqDone, MEASURE_RUNS, measParDone, MEASURE_RUNS);
            }

            var parQ = 0L;
            var parTotal = 0L;
            for (var r = 0; r < MEASURE_RUNS; r++)
            {
                var parStart = NanoTime();
                var parRes = finder.KosarajuParallel(graph);
                var parEnd = NanoTime();

                measParDone++;
                DrawProgress(genDone, warmSeqDone, WARMUP_RUNS, warmParDone, WARMUP_RUNS, measSeqDone, MEASURE_RUNS, measParDone, MEASURE_RUNS);

                parQ = parRes.Count;
                parTotal += (parEnd - parStart);
            }

            Console.Write("\r" + new string(' ', 50) + "\r");

            var seqAvg = seqTotal / (double)MEASURE_RUNS / 1_000_000.0;
            var parAvg = parTotal / (double)MEASURE_RUNS / 1_000_000.0;
            var acc = seqAvg / parAvg;

            var cmdOutput = string.Format(cmdPattern, n, edgeCount, seqAvg, parAvg, acc, parQ);
            var csvOutput = string.Format(csvPattern, n, edgeCount, seqAvg, parAvg, acc, parQ);

            Console.WriteLine(cmdOutput);
            File.AppendAllLines(decoPath, [cmdOutput]);
            File.AppendAllLines(purePath, [csvOutput]);
        }

        Console.WriteLine(separator);
        File.AppendAllLines(decoPath, [separator]);
    }

    private static long NanoTime()
    {
        var nano = 10000L * Stopwatch.GetTimestamp();
        nano /= TimeSpan.TicksPerMillisecond;
        nano *= 100L;
        return nano;
    }
    private static void DrawProgress(
        int genDone,
        int warmSeqDone, int warmSeqTotal,
        int warmParDone, int warmParTotal,
        int measSeqDone, int measSeqTotal,
        int measParDone, int measParTotal)
    {
        static string Segment(int done, int total)
        {
            return new string('■', done) + new string('-', total - done);
        }

        var bar =
            Segment(genDone, 1) + "|" +
            Segment(warmSeqDone, warmSeqTotal) + "|" +
            Segment(measSeqDone, measSeqTotal) + "|" +
            Segment(warmParDone, warmParTotal) + "|" +
            Segment(measParDone, measParTotal);

        var total =
            1 + warmSeqTotal + measSeqTotal + warmParTotal + measParTotal;

        var done =
            genDone + warmSeqDone + measSeqDone + warmParDone + measParDone;

        var percent = (double)done / total * 100;

        Console.Write($"\r[{bar}] {percent,6:F1}%");
    }
}
