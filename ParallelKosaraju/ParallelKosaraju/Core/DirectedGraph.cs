using System;
using System.Collections.Generic;
using System.Threading;

namespace ParallelKosaraju.Core
{
    /// <summary>
    /// Adjacency-list directed graph. Thread-safe for reads after construction.
    /// </summary>
    public sealed class DirectedGraph
    {
        public int VertexCount { get; }
        private int _edgeCount;
        public int EdgeCount => _edgeCount;

        // Forward adjacency lists
        private readonly List<int>[] _adj;
        // Reverse adjacency lists (transpose)
        private readonly List<int>[] _radj;

        public DirectedGraph(int n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
            VertexCount = n;
            _adj  = new List<int>[n];
            _radj = new List<int>[n];
            for (int i = 0; i < n; i++)
            {
                _adj[i]  = new List<int>();
                _radj[i] = new List<int>();
            }
        }

        public void AddEdge(int from, int to)
        {
            if ((uint)from >= (uint)VertexCount) throw new ArgumentOutOfRangeException(nameof(from));
            if ((uint)to   >= (uint)VertexCount) throw new ArgumentOutOfRangeException(nameof(to));
            _adj[from].Add(to);
            _radj[to].Add(from);
            Interlocked.Increment(ref _edgeCount);
        }

        /// <summary>Forward neighbours of vertex v.</summary>
        public IReadOnlyList<int> Neighbours(int v) => _adj[v];

        /// <summary>Reverse neighbours of vertex v (neighbours in transposed graph).</summary>
        public IReadOnlyList<int> ReverseNeighbours(int v) => _radj[v];

        /// <summary>Returns a snapshot of all edges as (from, to) pairs.</summary>
        public IEnumerable<(int From, int To)> Edges()
        {
            for (int u = 0; u < VertexCount; u++)
                foreach (int v in _adj[u])
                    yield return (u, v);
        }
    }
}
