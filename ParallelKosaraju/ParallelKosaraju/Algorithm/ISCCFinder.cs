namespace ParallelKosaraju.Algorithm;

public interface ISCCFinder<T>
{
    List<List<int>> KosarajuSequential(DirectedGraph<T> graph);
    List<List<int>> ModifiedKosarajuSequential(DirectedGraph<T> graph);
    List<List<int>> KosarajuParallel(DirectedGraph<T> graph, bool isMultithreaded = true);
}
