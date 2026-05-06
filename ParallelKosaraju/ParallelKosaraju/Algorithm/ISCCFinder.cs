namespace ParallelKosaraju.Algorithm;

public interface ISCCFinder<T>
{
    List<List<int>> KosarajuSequential(DirectedGraph<T> graph);
    List<List<int>> KosarajuSingleThreadedParallel(DirectedGraph<T> graph);
    List<List<int>> KosarajuParallel(DirectedGraph<T> graph, int maxDegreeOfParallelism);
}
