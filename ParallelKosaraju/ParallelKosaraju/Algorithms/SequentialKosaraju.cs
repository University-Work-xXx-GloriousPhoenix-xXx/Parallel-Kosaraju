using System;
using System.Collections.Generic;
using System.Diagnostics;
using ParallelKosaraju.Core;

namespace ParallelKosaraju.Algorithms
{
    /// <summary>
    /// Classic sequential Kosaraju–Sharir algorithm.
    /// Pass 1: iterative DFS on original graph → finish-order stack.
    /// Pass 2: iterative DFS on transposed graph in reverse finish order.
    /// Time: O(V + E), Space: O(V).
    /// </summary>
    public sealed class SequentialKosaraju
    {
        private readonly DirectedGraph _g;

        public SequentialKosaraju(DirectedGraph g) => _g = g;

        /// <summary>
        /// Returns component labels: result[v] = SCC index for vertex v.
        /// SCCs are numbered in topological order of the condensation DAG.
        /// </summary>
        public int[] Compute(out TimeSpan elapsed, IProgress<StepEvent>? progress = null)
        {
            var sw = Stopwatch.StartNew();
            int n = _g.VertexCount;

            // ── Pass 1: compute finish order on original graph ──────────────
            progress?.Report(new StepEvent(AlgoPhase.Pass1Start, -1, -1, null));
            var order = new int[n];          // finish order (stack output)
            int top   = 0;
            var visited = new bool[n];

            for (int s = 0; s < n; s++)
            {
                if (visited[s]) continue;
                Dfs1(s, visited, order, ref top, progress);
            }

            // ── Pass 2: assign SCCs on transposed graph ─────────────────────
            progress?.Report(new StepEvent(AlgoPhase.Pass2Start, -1, -1, null));
            var comp    = new int[n];
            Array.Fill(comp, -1);
            int sccId = 0;

            for (int i = top - 1; i >= 0; i--)
            {
                int s = order[i];
                if (comp[s] != -1) continue;
                Dfs2(s, sccId, comp, progress);
                sccId++;
            }

            sw.Stop();
            elapsed = sw.Elapsed;
            progress?.Report(new StepEvent(AlgoPhase.Done, -1, -1, null));
            return comp;
        }

        // Iterative DFS – pass 1 (forward graph)
        private void Dfs1(int start, bool[] visited, int[] order, ref int top,
                          IProgress<StepEvent>? progress)
        {
            // stack holds (vertex, iterator-index)
            var stack = new Stack<(int v, int idx)>();
            visited[start] = true;
            stack.Push((start, 0));
            progress?.Report(new StepEvent(AlgoPhase.Pass1Visit, start, -1, null));

            while (stack.Count > 0)
            {
                var (v, idx) = stack.Peek();
                var nb = _g.Neighbours(v);
                if (idx < nb.Count)
                {
                    stack.Pop();
                    stack.Push((v, idx + 1));
                    int w = nb[idx];
                    if (!visited[w])
                    {
                        visited[w] = true;
                        stack.Push((w, 0));
                        progress?.Report(new StepEvent(AlgoPhase.Pass1Visit, w, v, null));
                    }
                }
                else
                {
                    stack.Pop();
                    order[top++] = v;
                    progress?.Report(new StepEvent(AlgoPhase.Pass1Finish, v, -1, null));
                }
            }
        }

        // Iterative DFS – pass 2 (transposed graph)
        private void Dfs2(int start, int sccId, int[] comp,
                          IProgress<StepEvent>? progress)
        {
            var stack = new Stack<(int v, int idx)>();
            comp[start] = sccId;
            stack.Push((start, 0));
            progress?.Report(new StepEvent(AlgoPhase.Pass2Assign, start, sccId, null));

            while (stack.Count > 0)
            {
                var (v, idx) = stack.Peek();
                var nb = _g.ReverseNeighbours(v);
                if (idx < nb.Count)
                {
                    stack.Pop();
                    stack.Push((v, idx + 1));
                    int w = nb[idx];
                    if (comp[w] == -1)
                    {
                        comp[w] = sccId;
                        stack.Push((w, 0));
                        progress?.Report(new StepEvent(AlgoPhase.Pass2Assign, w, sccId, null));
                    }
                }
                else
                {
                    stack.Pop();
                }
            }
        }
    }
}
