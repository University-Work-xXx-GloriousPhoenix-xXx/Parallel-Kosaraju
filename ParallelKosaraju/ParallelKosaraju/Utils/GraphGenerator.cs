using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ParallelKosaraju.Core;

namespace ParallelKosaraju.Utils
{
    public enum GraphModel
    {
        ErdosRenyi,          // G(n,p) random
        BarabasiAlbert,      // Scale-free / preferential attachment
        RandomClusters,      // Dense clusters with sparse inter-cluster edges (many SCCs)
        CompleteTournament,  // Complete tournament — one big SCC
        Chain                // Simple chain — every vertex is its own SCC
    }

    public static class GraphGenerator
    {
        /// <summary>
        /// Generates a directed graph according to the chosen model.
        /// </summary>
        /// <param name="n">Number of vertices.</param>
        /// <param name="param">
        ///   ErdosRenyi  → edge probability (0..1).
        ///   BarabasiAlbert → edges added per new node (int).
        ///   RandomClusters → number of clusters (int).
        ///   CompleteTournament / Chain → ignored.
        /// </param>
        public static DirectedGraph Generate(GraphModel model, int n, double param = 0.01, int? seed = null)
        {
            var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;

            return model switch
            {
                GraphModel.ErdosRenyi        => Erdos(n, param, rng),
                GraphModel.BarabasiAlbert    => Barabasi(n, Math.Max(1, (int)param), rng),
                GraphModel.RandomClusters    => Clusters(n, Math.Max(2, (int)param), rng),
                GraphModel.CompleteTournament=> Tournament(n, rng),
                GraphModel.Chain             => Chain(n),
                _ => throw new ArgumentOutOfRangeException(nameof(model))
            };
        }

        // ── Erdős–Rényi G(n,p) ──────────────────────────────────────────────
        private static DirectedGraph Erdos(int n, double p, Random rng)
        {
            var g = new DirectedGraph(n);
            // For large n we iterate smartly using geometric skip
            if (p <= 0) return g;
            if (p >= 1)
            {
                for (int u = 0; u < n; u++)
                    for (int v = 0; v < n; v++)
                        if (u != v) g.AddEdge(u, v);
                return g;
            }
            double logQ = Math.Log(1 - p);
            for (int u = 0; u < n; u++)
            {
                int v = -1;
                while (true)
                {
                    v += 1 + (int)(Math.Log(1 - rng.NextDouble()) / logQ);
                    if (v == u) v++;
                    if (v >= n) break;
                    g.AddEdge(u, v);
                }
            }
            return g;
        }

        // ── Barabási–Albert preferential attachment ──────────────────────────
        private static DirectedGraph Barabasi(int n, int m, Random rng)
        {
            var g = new DirectedGraph(n);
            // degree list for preferential attachment
            var degrees = new List<int>(n * m * 2);
            // seed with small clique
            int seed = Math.Min(m + 1, n);
            for (int u = 0; u < seed; u++)
                for (int v = 0; v < seed; v++)
                    if (u != v) { g.AddEdge(u, v); degrees.Add(u); degrees.Add(v); }

            for (int i = seed; i < n; i++)
            {
                var targets = new HashSet<int>();
                while (targets.Count < Math.Min(m, i))
                {
                    int t = degrees[rng.Next(degrees.Count)];
                    if (t != i) targets.Add(t);
                }
                foreach (int t in targets)
                {
                    g.AddEdge(i, t);
                    degrees.Add(i); degrees.Add(t);
                    // also add reverse with probability 0.3 to create SCCs
                    if (rng.NextDouble() < 0.3)
                    {
                        g.AddEdge(t, i);
                        degrees.Add(t); degrees.Add(i);
                    }
                }
            }
            return g;
        }

        // ── Random clusters (many dense SCCs) ───────────────────────────────
        private static DirectedGraph Clusters(int n, int k, Random rng)
        {
            var g = new DirectedGraph(n);
            int clusterSize = n / k;
            // intra-cluster: high density directed cycle + random edges
            for (int c = 0; c < k; c++)
            {
                int start = c * clusterSize;
                int end   = (c == k - 1) ? n : start + clusterSize;
                int sz    = end - start;
                if (sz < 2) continue;
                // guaranteed cycle through cluster
                for (int i = start; i < end; i++)
                    g.AddEdge(i, start + (i - start + 1) % sz);
                // extra random intra-cluster edges (p~0.3)
                for (int u = start; u < end; u++)
                    for (int v = start; v < end; v++)
                        if (u != v && rng.NextDouble() < 0.3) g.AddEdge(u, v);
            }
            // inter-cluster: sparse (p~0.001) so SCCs stay mostly separate
            for (int u = 0; u < n; u++)
                for (int v = 0; v < n; v++)
                    if (u / clusterSize != v / clusterSize && rng.NextDouble() < 0.001)
                        g.AddEdge(u, v);
            return g;
        }

        // ── Complete tournament ──────────────────────────────────────────────
        private static DirectedGraph Tournament(int n, Random rng)
        {
            var g = new DirectedGraph(n);
            for (int u = 0; u < n; u++)
                for (int v = u + 1; v < n; v++)
                    if (rng.NextDouble() < 0.5) g.AddEdge(u, v); else g.AddEdge(v, u);
            return g;
        }

        // ── Simple chain ─────────────────────────────────────────────────────
        private static DirectedGraph Chain(int n)
        {
            var g = new DirectedGraph(n);
            for (int i = 0; i < n - 1; i++) g.AddEdge(i, i + 1);
            return g;
        }
    }
}
