namespace ParallelKosaraju.Algorithm;

public interface ISCCFinder<T>
{
    List<List<int>> BasicKosarajuSequential(DirectedGraph<T> graph);
    List<List<int>> ModifiedKosarajuSequential(DirectedGraph<T> graph);
    List<List<int>> ModifiedKosarajuParallel(DirectedGraph<T> graph, bool isMultithreaded = true);
}
