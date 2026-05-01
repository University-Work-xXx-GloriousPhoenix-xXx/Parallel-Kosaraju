using ParallelKosaraju.Algorithm;

namespace ParallelKosaraju.Tests;

public class TestSequential
{
    private static readonly SCCFinder<int> finder = new();

    [Fact]
    public void Sequential_WithEmptyGraph_ReturnsEmptyList()
    {
        var graph = GraphHelper.GenerateRandomGraph(0, 0);
        var result = finder.KosarajuSequential(graph);
        Assert.Equal([], result);
    }

    [Fact]
    public void Sequential_WithSingleVertex_ReturnsSingleComponent()
    {
        var graph = GraphHelper.GenerateRandomGraph(1, 0);
        var result = finder.KosarajuSequential(graph);
        Assert.Equal([[0]], result);
    }

    [Fact]
    public void Sequential_WithTwoDisconnectedVertices_ReturnsTwoComponents()
    {
        var graph = new DirectedGraph<int>(2);
        var result = finder.KosarajuSequential(graph);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Sequential_WithSimpleCycle_ReturnsOneComponent()
    {
        var graph = new DirectedGraph<int>(3);
        graph.AddEdge(0, 1);
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 0);
        var result = finder.KosarajuSequential(graph);
        Assert.Single(result);
        Assert.Equal(3, result[0].Count);
    }

    [Fact]
    public void Sequential_WithLinearChain_ReturnsEachVertexAsSeparateComponent()
    {
        var graph = new DirectedGraph<int>(4);
        graph.AddEdge(0, 1);
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        var result = finder.KosarajuSequential(graph);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Sequential_WithTwoSeparateCycles_ReturnsTwoComponents()
    {
        var graph = new DirectedGraph<int>(4);
        graph.AddEdge(0, 1);
        graph.AddEdge(1, 0);
        graph.AddEdge(2, 3);
        graph.AddEdge(3, 2);
        var result = finder.KosarajuSequential(graph);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Sequential_WithFullyConnectedGraph_ReturnsOneComponent()
    {
        var graph = new DirectedGraph<int>(4);
        for (var i = 0; i < 4; i++)
            for (var j = 0; j < 4; j++)
                if (i != j) graph.AddEdge(i, j);
        var result = finder.KosarajuSequential(graph);
        Assert.Single(result);
        Assert.Equal(4, result[0].Count);
    }

    [Fact]
    public void Sequential_ComponentSizesAreCorrect()
    {
        var graph = new DirectedGraph<int>(3);
        graph.AddEdge(0, 1);
        graph.AddEdge(1, 0);
        var result = finder.KosarajuSequential(graph);
        var sizes = result.Select(c => c.Count).OrderByDescending(x => x).ToList();
        Assert.Equal([2, 1], sizes);
    }

    [Fact]
    public void Sequential_WithThreeComponents_ReturnsThreeComponents()
    {
        var graph = new DirectedGraph<int>(8);

        graph.AddEdge(7, 6);

        graph.AddEdge(6, 5);
        graph.AddEdge(5, 4);
        graph.AddEdge(4, 6);

        graph.AddEdge(4, 2);

        graph.AddEdge(2, 1);
        graph.AddEdge(1, 0);
        graph.AddEdge(0, 3);
        graph.AddEdge(3, 2);

        var result = finder.KosarajuSequential(graph);
        var sortedResult = result
            .Select(x => x
                .Order()
                .ToList())
            .OrderBy(x => x.Count)
            .ToList();

        Assert.Equal(3, sortedResult.Count);
        Assert.Equal([[7], [4, 5, 6], [0, 1, 2, 3]], sortedResult);
    }
}
