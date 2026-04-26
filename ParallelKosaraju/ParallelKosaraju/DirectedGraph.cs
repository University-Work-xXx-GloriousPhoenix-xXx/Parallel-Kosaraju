namespace ParallelKosaraju;

public class DirectedGraph<T>
{
    public int VertexCount { get; }
    public List<int>[] OutEdges { get; }
    public List<int>[] InEdges { get; }
    public T[] Vertices { get; }

    public DirectedGraph(int n)
    {
        VertexCount = n;
        OutEdges = new List<int>[n];
        InEdges = new List<int>[n];
        Vertices = new T[n];
        for (var i = 0; i < n; i++)
        {
            OutEdges[i] = [];
            InEdges[i] = [];
        }
    }

    public DirectedGraph(T[] vertices) : this(vertices.Length)
    {
        vertices.CopyTo(Vertices, 0);
    }

    public void AddEdge(int from, int to)
    {
        OutEdges[from].Add(to);
        InEdges[to].Add(from);
    }
}

