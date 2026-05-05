using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace ParallelKosaraju.Algorithm;

public class SCCFinder<T> : ISCCFinder<T>
{
    public List<List<int>> KosarajuSequential(DirectedGraph<T> graph)
        => PerformKosaraju(graph, maxDegreeOfParallelism: 1);

    public List<List<int>> KosarajuParallel(DirectedGraph<T> graph, int maxDegreeOfParallelism = -1)
        => PerformKosaraju(graph, maxDegreeOfParallelism);

    private static List<List<int>> PerformKosaraju(DirectedGraph<T> graph, int maxDegreeOfParallelism)
    {
        var n = graph.VertexCount;
        var order = PerformPhase1(graph, n);
        return PerformPhase2(graph, n, order, maxDegreeOfParallelism);
    }

    private static List<int> PerformPhase1(DirectedGraph<T> graph, int n)
    {
        var state = new byte[n];
        var stack = new Stack<int>();
        var order = new List<int>(n);

        for (var v = 0; v < n; v++)
        {
            if (state[v] != 0) continue;
            stack.Push(v);
            while (stack.Count > 0)
            {
                var node = stack.Peek();
                if (state[node] == 0)
                {
                    state[node] = 1;
                    foreach (var neighbour in graph.OutEdges[node])
                        if (state[neighbour] == 0)
                            stack.Push(neighbour);
                }
                else
                {
                    if (state[node] == 1)
                    {
                        state[node] = 2;
                        order.Add(node);
                    }
                    stack.Pop();
                }
            }
        }

        return order;
    }

    private static List<List<int>> PerformPhase2(
        DirectedGraph<T> graph, int n, List<int> order, int maxDegreeOfParallelism)
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