namespace ParallelKosaraju;

public class DirectedGraph
{
    public int VertexCount { get; }
    public List<int>[] OutEdges { get; } // Outcoming edges
    public List<int>[] InEdges { get; } // Incoming (reversed graph)

    public DirectedGraph(int n)
    {
        VertexCount = n;
        OutEdges = new List<int>[n];
        InEdges = new List<int>[n];
        for (int i = 0; i < n; i++)
        {
            OutEdges[i] = [];
            InEdges[i] = [];
        }
    }

    public void AddEdge(int from, int to)
    {
        OutEdges[from].Add(to);
        InEdges[to].Add(from);
    }
}