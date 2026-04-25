using System.Collections.Concurrent;

namespace ParallelKosaraju;

public static class SCCFinder
{
    public static List<List<int>> KosarajuSequential(DirectedGraph graph)
    {
        int n = graph.VertexCount;
        var visited = new bool[n];
        var order = new List<int>(n);

        // ---------- Первый проход (прямой граф) ----------
        // Итеративный DFS с состояниями: 0 – не начат, 1 – в процессе, 2 – обработан
        var state = new byte[n]; // 0,1,2
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
                    // первый вход – помечаем как обрабатываемый
                    state[node] = 1;
                    foreach (int neighbor in graph.OutEdges[node])
                    {
                        if (state[neighbor] == 0)
                            stack.Push(neighbor);
                    }
                }
                else if (state[node] == 1)
                {
                    // все соседи обработаны – завершаем узел
                    state[node] = 2;
                    order.Add(node);
                    stack.Pop();
                }
                else // 2 – уже обработан, просто снимаем
                {
                    stack.Pop();
                }
            }
        }

        // ---------- Второй проход (обратный граф) ----------
        var comps = new List<List<int>>();
        var visited2 = new bool[n];

        for (int i = order.Count - 1; i >= 0; i--)
        {
            int start = order[i];
            if (visited2[start]) continue;

            // DFS на обратном графе
            var comp = new List<int>();
            var dfsStack = new Stack<int>();
            dfsStack.Push(start);
            visited2[start] = true;

            while (dfsStack.Count > 0)
            {
                int node = dfsStack.Pop();
                comp.Add(node);

                foreach (int neighbor in graph.InEdges[node])
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
    public static List<List<int>> KosarajuParallel(DirectedGraph graph)
    {
        int n = graph.VertexCount;
        var visited = new bool[n];
        var order = new List<int>(n);

        // ---------- Первый проход (последовательный) ----------
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

        // ---------- Второй проход (параллельный) ----------
        var comps = new ConcurrentBag<List<int>>();
        // 0 – не посещена, 1 – посещена
        var visitedFlags = new int[n]; // инициализированы 0

        // общий индекс для обхода order в обратном порядке
        int nextIndex = order.Count - 1;

        // Пул потоков будет брать следующую непосещённую вершину
        Parallel.For(0, Environment.ProcessorCount, _ =>
        {
            while (true)
            {
                int idx = Interlocked.Decrement(ref nextIndex);
                if (idx < 0) break; // все вершины распределены

                int start = order[idx];

                // Атомарно проверяем, не посещена ли вершина, и помечаем
                if (Interlocked.CompareExchange(ref visitedFlags[start], 1, 0) != 0)
                    continue; // уже взята другим потоком

                // Начинаем DFS с этой вершины
                var comp = new List<int>();
                var localStack = new Stack<int>();
                localStack.Push(start);

                while (localStack.Count > 0)
                {
                    int node = localStack.Pop();
                    comp.Add(node);

                    foreach (int neighbor in graph.InEdges[node])
                    {
                        // Пытаемся завладеть соседом
                        if (Interlocked.CompareExchange(ref visitedFlags[neighbor], 1, 0) == 0)
                        {
                            localStack.Push(neighbor);
                        }
                    }
                }
                comps.Add(comp);
            }
        });

        return comps.ToList();
    }
}
