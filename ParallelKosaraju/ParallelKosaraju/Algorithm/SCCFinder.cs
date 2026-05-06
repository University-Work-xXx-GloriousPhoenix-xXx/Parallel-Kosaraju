using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace ParallelKosaraju.Algorithm;

public class SCCFinder<T> : ISCCFinder<T>
{
    public List<List<int>> KosarajuSequential(
        DirectedGraph<T> graph)
        => PerformKosaraju(graph, maxDegreeOfParallelism: 1);

    public List<List<int>> KosarajuParallel(
        DirectedGraph<T> graph,
        int maxDegreeOfParallelism = -1)
        => PerformKosaraju(graph, maxDegreeOfParallelism);

    private static List<List<int>> PerformKosaraju(
        DirectedGraph<T> graph,
        int maxDegreeOfParallelism)
    {
        var n = graph.VertexCount;
        var order = PerformPhase1(graph, n, maxDegreeOfParallelism);
        return PerformPhase2(graph, n, order, maxDegreeOfParallelism);
    }

    private static List<int> PerformPhase1(
        DirectedGraph<T> graph,
        int n,
        int maxDegreeOfParallelism)
    {
        var visited = new int[n];
        var sharedOrder = new ConcurrentStack<int>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };

        Parallel.For(0, n, options, v =>
        {
            if (Interlocked.CompareExchange(ref visited[v], 1, 0) == 0)
            {
                var localStack = new Stack<int>();
                var localOrder = new List<int>();

                var dfsStack = new Stack<(int node, int edgeIndex)>();
                dfsStack.Push((v, 0));

                while (dfsStack.Count > 0)
                {
                    var (curr, edgeIdx) = dfsStack.Pop();
                    var neighbors = graph.OutEdges[curr];

                    if (edgeIdx < neighbors.Count)
                    {
                        dfsStack.Push((curr, edgeIdx + 1));
                        int neighbor = neighbors[edgeIdx];

                        if (Interlocked.CompareExchange(ref visited[neighbor], 1, 0) == 0)
                        {
                            dfsStack.Push((neighbor, 0));
                        }
                    }
                    else
                    {
                        sharedOrder.Push(curr);
                    }
                }
            }
        });

        return [.. sharedOrder.Reverse()];
    }

    private static List<List<int>> PerformPhase2(
        DirectedGraph<T> graph,
        int n,
        List<int> order,
        int maxDegreeOfParallelism)
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
        var visited = new bool[n];
        var components = new List<List<int>>();

        for (var i = order.Count - 1; i >= 0; i--)
        {
            var start = order[i];
            if (visited[start]) continue;

            visited[start] = true;

            var component = new ConcurrentBag<int> { start };
            var currentFrontier = new List<int> { start };

            while (currentFrontier.Count > 0)
            {
                var nextFrontier = new ConcurrentBag<int>();

                Parallel.ForEach(currentFrontier, options, node =>
                {
                    foreach (var neighbour in graph.InEdges[node])
                    {
                        if (!visited[neighbour] &&
                            Interlocked.Exchange(
                                ref Unsafe.As<bool, byte>(ref visited[neighbour]), 1) == 0)
                        {
                            component.Add(neighbour);
                            nextFrontier.Add(neighbour);
                        }
                    }
                });

                currentFrontier = [.. nextFrontier];
            }

            components.Add([.. component]);
        }

        return components;
    }
}