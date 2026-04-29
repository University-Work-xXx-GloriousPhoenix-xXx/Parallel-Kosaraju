using System.Collections.Concurrent;

namespace ParallelKosaraju.Algorithm;

public partial class SCCFinder<T> : ISCCFinder<T>
{
    public List<List<int>> ModifiedKosarajuParallel(DirectedGraph<T> graph, bool isMultithreaded = true)
    {
        var n = graph.VertexCount;
        if (n == 0) return [];

        // 1. Разбиение на k партиций — жадно по числу рёбер
        var partitions = PartitionByEdgeCount(graph, isMultithreaded
            ? Environment.ProcessorCount
            : 1);

        var finalComponents = new ConcurrentBag<List<int>>();
        var boundaryVerticesBag = new ConcurrentBag<int>();

        // 2. Параллельный локальный Косараджу на каждой партиции
        Parallel.ForEach(partitions,
            new ParallelOptions { MaxDegreeOfParallelism = isMultithreaded ? -1 : 1 },
            partition =>
            {
                var localSccs = RunLocalKosaraju(graph, partition);

                // 3. Классифицируем локальные SCC
                foreach (var scc in localSccs)
                {
                    if (IsFinalComponent(graph, scc, partition))
                    {
                        finalComponents.Add(scc);
                    }
                    else
                    {
                        // Граничные — вершины пойдут во второй проход
                        foreach (var v in scc)
                            boundaryVerticesBag.Add(v);
                    }
                }
            });

        // 4. Рекурсивный Косараджу на граничных вершинах
        var boundaryVertices = boundaryVerticesBag.ToHashSet();
        if (boundaryVertices.Count > 0)
        {
            var boundarySccs = RunLocalKosaraju(graph, boundaryVertices);
            foreach (var scc in boundarySccs)
                finalComponents.Add(scc);
        }

        return [.. finalComponents];
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Разбиение по числу рёбер (out-degree) — жадный алгоритм
    // ──────────────────────────────────────────────────────────────────────────
    private static List<HashSet<int>> PartitionByEdgeCount(DirectedGraph<T> graph, int k)
    {
        var n = graph.VertexCount;
        k = Math.Min(k, n);

        // Сортируем вершины по убыванию out-degree (largest-first для лучшего баланса)
        var vertices = Enumerable.Range(0, n)
            .OrderByDescending(v => graph.OutEdges[v].Count)
            .ToList();

        var partitions = Enumerable.Range(0, k)
            .Select(_ => new HashSet<int>())
            .ToList();

        // Отслеживаем суммарный вес (число рёбер) каждой партиции
        var loads = new long[k];

        foreach (var v in vertices)
        {
            // Кладём вершину в наименее загруженную партицию
            var minIdx = Array.IndexOf(loads, loads.Min());
            partitions[minIdx].Add(v);
            loads[minIdx] += graph.OutEdges[v].Count;
        }

        return partitions;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Локальный Косараджу — работает только с вершинами из vertexMask,
    // но использует оригинальные рёбра (cross-edges просто игнорируются
    // в рамках DFS, но их наличие фиксируется для классификации)
    // ──────────────────────────────────────────────────────────────────────────
    private static List<List<int>> RunLocalKosaraju(
        DirectedGraph<T> graph,
        IReadOnlySet<int> vertexMask)
    {
        var state = new Dictionary<int, byte>(vertexMask.Count);
        foreach (var v in vertexMask) state[v] = 0;

        var stack = new Stack<int>();
        var order = new List<int>();

        // Forward DFS — обходим только вершины внутри маски
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
                        // Переходим только внутри маски
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

        // Backward DFS — в обратном порядке финализации
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

    // ──────────────────────────────────────────────────────────────────────────
    // Компонента финальная, если ни одна вершина не имеет рёбра
    // наружу (в вершины вне партиции)
    // ──────────────────────────────────────────────────────────────────────────
    private static bool IsFinalComponent(
        DirectedGraph<T> graph,
        List<int> scc,
        IReadOnlySet<int> partition)
    {
        var sccSet = new HashSet<int>(scc);
        foreach (var v in scc)
        {
            foreach (var neighbour in graph.OutEdges[v])
            {
                // Cross-edge в другую партицию — компонента граничная
                if (!partition.Contains(neighbour))
                    return false;

                // Ребро выходит за пределы SCC внутри той же партиции —
                // тоже граничная: Косараджу мог объединить вершины
                // только потому что путь шёл через "чужие" вершины маски
                if (!sccSet.Contains(neighbour))
                    return false;
            }
        }
        return true;
    }
}