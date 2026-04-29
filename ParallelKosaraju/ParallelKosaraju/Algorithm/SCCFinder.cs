using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace ParallelKosaraju.Algorithm;

public class SCCFinder<T> : ISCCFinder<T>
{
    public List<List<int>> KosarajuSequential(DirectedGraph<T> graph)
    {
        var n = graph.VertexCount;

        var state = new byte[n];
        var stack = new Stack<int>();
        var order = new List<int>();

        // Forward DFS
        for (var v = 0; v < n; v++)
        {
            if (state[v] != 0)
            {
                continue;
            }
            stack.Push(v);
            while (stack.Count > 0)
            {
                var node = stack.Peek();
                if (state[node] == 0)
                {
                    state[node] = 1;
                    foreach (var neighbour in graph.OutEdges[node])
                    {
                        if (state[neighbour] == 0)
                        {
                            stack.Push(neighbour);
                        }
                    }
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

        var components = new List<List<int>>();
        var visited = new bool[n];

        // Backward DFS
        for (var i = order.Count - 1; i >= 0; i--)
        {
            var start = order[i];
            if (visited[start])
            {
                continue;
            }

            var component = new List<int>();
            stack.Push(start);
            visited[start] = true;

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                component.Add(node);
                foreach (var neighbour in graph.InEdges[node])
                {
                    if (!visited[neighbour])
                    {
                        visited[neighbour] = true;
                        stack.Push(neighbour);
                    }
                }
            }

            components.Add(component);
        }

        return components;
    }

    public List<List<int>> KosarajuParallel(DirectedGraph<T> graph)
    {
        var n = graph.VertexCount;
        var order = ComputeFinishOrderSequential(graph, n);
        return BuildComponentsParallel(graph, n, order);
    }

    private static List<int> ComputeFinishOrderSequential(DirectedGraph<T> graph, int n)
    {
        var state = new byte[n];
        var stack = new Stack<int>();
        var order = new List<int>();

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

    private static List<List<int>> BuildComponentsParallel(
        DirectedGraph<T> graph, int n, List<int> order)
    {
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

                Parallel.ForEach(currentFrontier, node =>
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