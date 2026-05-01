using ParallelKosaraju.Algorithm;

namespace ParallelKosaraju.Tests;

public class TestParallel
{
    private static readonly SCCFinder<int> finder = new();

    [Fact]
    public void Parallel_WithEmptyGraph_ReturnsEmptyList()
    {
        var graph = GraphHelper.GenerateRandomGraph(0, 0);
        var result = finder.KosarajuParallel(graph);
        Assert.Equal([], result);
    }

    [Fact]
    public void Parallel_WithSingleVertex_ReturnsSingleComponent()
    {
        var graph = GraphHelper.GenerateRandomGraph(1, 0);
        var result = finder.KosarajuParallel(graph);
        Assert.Equal([[0]], result);
    }

    [Fact]
    public void Parallel_WithSimpleCycle_ReturnsOneComponent()
    {
        var graph = new DirectedGraph<int>(3);
        graph.AddEdge(0, 1);
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 0);
        var result = finder.KosarajuParallel(graph);
        Assert.Single(result);
        Assert.Equal(3, result[0].Count);
    }

    [Fact]
    public void Parallel_WithLinearChain_ReturnsEachVertexAsSeparateComponent()
    {
        var graph = new DirectedGraph<int>(4);
        graph.AddEdge(0, 1);
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        var result = finder.KosarajuParallel(graph);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Parallel_WithTwoSeparateCycles_ReturnsTwoComponents()
    {
        var graph = new DirectedGraph<int>(4);
        graph.AddEdge(0, 1);
        graph.AddEdge(1, 0);
        graph.AddEdge(2, 3);
        graph.AddEdge(3, 2);
        var result = finder.KosarajuParallel(graph);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Parallel_WithSelfLoop_ReturnsSingleComponent()
    {
        var graph = new DirectedGraph<int>(1);
        graph.AddEdge(0, 0);
        var result = finder.KosarajuParallel(graph);
        Assert.Single(result);
        Assert.Equal([0], result[0]);
    }

    [Fact]
    public void Parallel_ComponentCountMatchesSequential()
    {
        var graph = GraphHelper.GenerateRandomGraph(100, 300);
        var sequential = finder.KosarajuSequential(graph);
        var parallel = finder.KosarajuParallel(graph);
        Assert.Equal(sequential.Count, parallel.Count);
    }

    [Fact]
    public void Parallel_ComponentSizesMatchSequential()
    {
        var graph = GraphHelper.GenerateRandomGraph(200, 600);
        var sequential = finder.KosarajuSequential(graph)
            .Select(c => c.Count).OrderByDescending(x => x).ToList();
        var parallel = finder.KosarajuParallel(graph)
            .Select(c => c.Count).OrderByDescending(x => x).ToList();
        Assert.Equal(sequential, parallel);
    }

    [Fact]
    public void Parallel_VertexSetMatchesSequential()
    {
        var graph = GraphHelper.GenerateRandomGraph(150, 400);
        var sequential = finder.KosarajuSequential(graph)
            .SelectMany(c => c).OrderBy(x => x).ToList();
        var parallel = finder.KosarajuParallel(graph)
            .SelectMany(c => c).OrderBy(x => x).ToList();
        Assert.Equal(sequential, parallel);
    }
}
