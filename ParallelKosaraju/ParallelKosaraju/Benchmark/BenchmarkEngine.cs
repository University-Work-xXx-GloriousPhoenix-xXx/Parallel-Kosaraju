using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ParallelKosaraju.Algorithms;
using ParallelKosaraju.Core;
using ParallelKosaraju.Utils;

namespace ParallelKosaraju.Benchmark
{
    public sealed class BenchmarkResult
    {
        public string      Label          { get; init; } = "";
        public int         Vertices       { get; init; }
        public int         Edges          { get; init; }
        public GraphModel  Model          { get; init; }
        public TimeSpan    SeqTime        { get; init; }
        public TimeSpan    ParTime        { get; init; }
        public double      Speedup        => SeqTime.TotalMilliseconds / ParTime.TotalMilliseconds;
        public int         SccCountSeq    { get; init; }
        public int         SccCountPar    { get; init; }
        public bool        ResultsMatch   { get; init; }
    }

    public static class BenchmarkEngine
    {
        /// <summary>
        /// Runs a full benchmark suite: several graph sizes × models.
        /// Reports progress via <paramref name="progress"/>.
        /// </summary>
        public static List<BenchmarkResult> Run(
            IProgress<string>? progress = null,
            bool warmup = true)
        {
            var results = new List<BenchmarkResult>();

            var configs = new (GraphModel model, int n, double param)[]
            {
                // ── Small smoke tests ──────────────────────────────
                (GraphModel.RandomClusters,    5_000,  20),
                (GraphModel.ErdosRenyi,        5_000,  0.005),
                // ── Medium ────────────────────────────────────────
                (GraphModel.RandomClusters,   50_000,  50),
                (GraphModel.ErdosRenyi,       50_000,  0.0005),
                (GraphModel.BarabasiAlbert,   50_000,  3),
                // ── Large — should show 1.2×+ speedup ─────────────
                (GraphModel.RandomClusters,  200_000,  100),
                (GraphModel.ErdosRenyi,      200_000,  0.00015),
                (GraphModel.BarabasiAlbert,  200_000,  5),
                // ── Extra-large ───────────────────────────────────
                (GraphModel.RandomClusters,  500_000,  200),
                (GraphModel.ErdosRenyi,      500_000,  0.00005),
            };

            if (warmup)
            {
                progress?.Report("[Warmup] Running warmup passes...");
                var wg = GraphGenerator.Generate(GraphModel.RandomClusters, 1000, 5, 42);
                new SequentialKosaraju(wg).Compute(out _);
                new Algorithms.ParallelKosaraju(wg).Compute(out _);
                progress?.Report("[Warmup] Done.");
            }

            foreach (var (model, n, param) in configs)
            {
                string label = $"{model} n={n:N0} p/k={param}";
                progress?.Report($"Generating: {label}");
                var g = GraphGenerator.Generate(model, n, param, seed: 12345);
                progress?.Report($"Running seq/par on {label} (E={g.EdgeCount:N0})...");

                // ── Sequential (3 runs, take median) ──────────────
                var seqTimes = new List<TimeSpan>(3);
                int[] seqComp = Array.Empty<int>();
                for (int r = 0; r < 3; r++)
                {
                    var alg = new SequentialKosaraju(g);
                    seqComp = alg.Compute(out var t);
                    seqTimes.Add(t);
                }
                seqTimes.Sort();
                var seqMedian = seqTimes[1];

                // ── Parallel (3 runs, take median) ─────────────────
                var parTimes = new List<TimeSpan>(3);
                int[] parComp = Array.Empty<int>();
                for (int r = 0; r < 3; r++)
                {
                    var alg = new Algorithms.ParallelKosaraju(g);
                    parComp = alg.Compute(out var t);
                    parTimes.Add(t);
                }
                parTimes.Sort();
                var parMedian = parTimes[1];

                // ── SCC count comparison ───────────────────────────
                int seqScc = seqComp.Length > 0 ? seqComp.Max() + 1 : 0;
                int parScc = parComp.Length > 0 ? parComp.Max() + 1 : 0;

                results.Add(new BenchmarkResult
                {
                    Label        = label,
                    Vertices     = n,
                    Edges        = g.EdgeCount,
                    Model        = model,
                    SeqTime      = seqMedian,
                    ParTime      = parMedian,
                    SccCountSeq  = seqScc,
                    SccCountPar  = parScc,
                    ResultsMatch = seqScc == parScc
                });

                double speedup = seqMedian.TotalMilliseconds / parMedian.TotalMilliseconds;
                progress?.Report(
                    $"  Seq={seqMedian.TotalMilliseconds:F1}ms  " +
                    $"Par={parMedian.TotalMilliseconds:F1}ms  " +
                    $"Speedup={speedup:F2}×  " +
                    $"SCCs seq={seqScc} par={parScc} match={seqScc == parScc}");
            }

            return results;
        }
    }
}
