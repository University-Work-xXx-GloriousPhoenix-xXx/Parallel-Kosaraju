using System.Collections.Concurrent;

namespace ParallelKosaraju.Algorithm;

public class SCCFinder<T> : ISCCFinder<T>
{
    public List<List<int>> BasicKosarajuSequential(DirectedGraph<T> graph)
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
    public List<List<int>> ModifiedKosarajuSequential(DirectedGraph<T> graph)
        => ModifiedKosarajuParallel(graph, isMultithreaded: false);
    public List<List<int>> ModifiedKosarajuParallel(DirectedGraph<T> graph, bool isMultithreaded = true)
    {
        var n = graph.VertexCount;
        if (n == 0) return [];

        var partitions = PartitionByEdgeCount(graph, isMultithreaded
            ? Environment.ProcessorCount
            : 1);

        var finalComponents = new ConcurrentBag<List<int>>();
        var boundaryVerticesBag = new ConcurrentBag<int>();

        Parallel.ForEach(partitions,
            new ParallelOptions { MaxDegreeOfParallelism = isMultithreaded ? -1 : 1 },
            partition =>
            {
                var localSccs = RunLocalKosaraju(graph, partition);

                foreach (var scc in localSccs)
                {
                    if (IsFinalComponent(graph, scc, partition))
                    {
                        finalComponents.Add(scc);
                    }
                    else
                    {
                        foreach (var v in scc)
                            boundaryVerticesBag.Add(v);
                    }
                }
            });

        var boundaryVertices = boundaryVerticesBag.ToHashSet();
        if (boundaryVertices.Count > 0)
        {
            var boundarySccs = RunLocalKosaraju(graph, boundaryVertices);
            foreach (var scc in boundarySccs)
                finalComponents.Add(scc);
        }

        return [.. finalComponents];
    }
    private static List<HashSet<int>> PartitionByEdgeCount(DirectedGraph<T> graph, int k)
    {
        var n = graph.VertexCount;
        k = Math.Min(k, n);

        var vertices = Enumerable.Range(0, n)
            .OrderByDescending(v => graph.OutEdges[v].Count)
            .ToList();

        var partitions = Enumerable.Range(0, k)
            .Select(_ => new HashSet<int>())
            .ToList();

        var loads = new long[k];

        foreach (var v in vertices)
        {
            var minIdx = Array.IndexOf(loads, loads.Min());
            partitions[minIdx].Add(v);
            loads[minIdx] += graph.OutEdges[v].Count;
        }

        return partitions;
    }
    private static List<List<int>> RunLocalKosaraju(DirectedGraph<T> graph, HashSet<int> vertexMask)
    {
        var state = new Dictionary<int, byte>(vertexMask.Count);
        foreach (var v in vertexMask) state[v] = 0;

        var stack = new Stack<int>();
        var order = new List<int>();

        foreach (var start in vertexMask)
        {
            if (state[start] != 0) continue;
            stack.Push(start);
            while (stack.Count > 0)
            {
                var node = stack.Peek();
                if (state[node] == 0)
                {
                    state[node] = 1;
                    foreach (var neighbour in graph.OutEdges[node])
                    {
                        if (vertexMask.Contains(neighbour) && state[neighbour] == 0)
                            stack.Push(neighbour);
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

        var visited = new Dictionary<int, bool>(vertexMask.Count);
        foreach (var v in vertexMask) visited[v] = false;

        var components = new List<List<int>>();

        for (var i = order.Count - 1; i >= 0; i--)
        {
            var start = order[i];
            if (visited[start]) continue;

            var component = new List<int>();
            stack.Push(start);
            visited[start] = true;

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                component.Add(node);
                foreach (var neighbour in graph.InEdges[node])
                {
                    if (vertexMask.Contains(neighbour) && !visited[neighbour])
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
    private static bool IsFinalComponent(DirectedGraph<T> graph, List<int> scc, HashSet<int> partition)
    {
        var sccSet = new HashSet<int>(scc);
        foreach (var v in scc)
        {
            foreach (var neighbour in graph.OutEdges[v])
            {
                if (!partition.Contains(neighbour))
                    return false;

                if (!sccSet.Contains(neighbour))
                    return false;
            }
        }
        return true;
    }
}