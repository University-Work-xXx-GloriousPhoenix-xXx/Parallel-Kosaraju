using System.Collections.Concurrent;

namespace ParallelKosaraju;

public static class SCCFinder
{
    public static List<List<int>> KosarajuSequential<T>(DirectedGraph<T> graph)
    {
        int n = graph.VertexCount;
        var visited = new bool[n];
        var order = new List<int>(n);

        // Direct Graph
        var state = new byte[n]; // 0,1,2
        var stack = new Stack<int>();

        for (var v = 0; v < n; v++)
        {
            if (state[v] != 0) continue;

            stack.Push(v);
            while (stack.Count > 0)
            {
                int node = stack.Peek();
                if (state[node] == 0)
                {
                    // First enter – mark as in process
                    state[node] = 1;
                    foreach (int neighbor in graph.OutEdges[node])
                    {
                        if (state[neighbor] == 0)
                            stack.Push(neighbor);
                    }
                }
                else if (state[node] == 1)
                {
                    // All neighbours processed - completing a node
                    state[node] = 2;
                    order.Add(node);
                    stack.Pop();
                }
                else // 2 – Already processed - removing
                {
                    stack.Pop();
                }
            }
        }

        // ---------- Inverse Graph ----------
        var comps = new List<List<int>>();
        var visited2 = new bool[n];

        for (int i = order.Count - 1; i >= 0; i--)
        {
            int start = order[i];
            if (visited2[start]) continue;

            // DFS on a Inverse Graph
            var comp = new List<int>();
            var dfsStack = new Stack<int>();
            dfsStack.Push(start);
            visited2[start] = true;

            while (dfsStack.Count > 0)
            {
                var node = dfsStack.Pop();
                comp.Add(node);

                foreach (var neighbor in graph.InEdges[node])
                {
                    if (!visited2[neighbor])
                    {
                        visited2[neighbor] = true;
                        dfsStack.Push(neighbor);
                    }
                }
            }
            comps.Add(comp);
        }

        return comps;
    }
    public static List<List<int>> KosarajuParallel<T>(DirectedGraph<T> graph)
    {
        int n = graph.VertexCount;
        var visited = new bool[n];
        var order = new List<int>(n);

        // ---------- First pass (Sequential) ----------
        var state = new byte[n];
        var stack = new Stack<int>();
        for (int v = 0; v < n; v++)
        {
            if (state[v] != 0) continue;
            stack.Push(v);
            while (stack.Count > 0)
            {
                int node = stack.Peek();
                if (state[node] == 0)
                {
                    state[node] = 1;
                    foreach (int neighbor in graph.OutEdges[node])
                        if (state[neighbor] == 0)
                            stack.Push(neighbor);
                }
                else if (state[node] == 1)
                {
                    state[node] = 2;
                    order.Add(node);
                    stack.Pop();
                }
                else
                {
                    stack.Pop();
                }
            }
        }

        // ---------- Second pass (Parallel) ----------
        var comps = new ConcurrentBag<List<int>>();
        // 0 – not visited, 1 – visited
        var visitedFlags = new int[n]; // init 0

        // shared index for bypass "order" in reverse order
        int nextIndex = order.Count - 1;

        // thread pool takes next unvisited vertex
        Parallel.For(0, Environment.ProcessorCount, _ =>
        {
            while (true)
            {
                int idx = Interlocked.Decrement(ref nextIndex);
                if (idx < 0) break; // all vertices distributed

                int start = order[idx];

                // atomic check of visit & marking
                if (Interlocked.CompareExchange(ref visitedFlags[start], 1, 0) != 0)
                    continue; // Already taken by other thread

                // Starting of DFS from this vertex
                var comp = new List<int>();
                var localStack = new Stack<int>();
                localStack.Push(start);

                while (localStack.Count > 0)
                {
                    int node = localStack.Pop();
                    comp.Add(node);

                    foreach (int neighbor in graph.InEdges[node])
                    {
                        // Trying to own a neighbour
                        if (Interlocked.CompareExchange(ref visitedFlags[neighbor], 1, 0) == 0)
                        {
                            localStack.Push(neighbor);
                        }
                    }
                }
                comps.Add(comp);
            }
        });

        return [.. comps];
    }
}
