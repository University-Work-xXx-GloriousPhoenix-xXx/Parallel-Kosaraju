namespace ParallelKosaraju.Algorithms
{
    public enum AlgoPhase
    {
        Pass1Start,
        Pass1Visit,
        Pass1Finish,
        Pass2Start,
        Pass2Assign,
        Done
    }

    /// <summary>
    /// Represents a single step emitted during sequential algorithm execution.
    /// Used by the step-by-step visualiser.
    /// </summary>
    public sealed class StepEvent
    {
        public AlgoPhase Phase      { get; }
        public int       Vertex     { get; }   // primary vertex
        public int       Extra      { get; }   // parent vertex (Pass1) or sccId (Pass2)
        public int[]?    FullState  { get; }   // optional snapshot

        public StepEvent(AlgoPhase phase, int vertex, int extra, int[]? fullState)
        {
            Phase     = phase;
            Vertex    = vertex;
            Extra     = extra;
            FullState = fullState;
        }

        public override string ToString() => Phase switch
        {
            AlgoPhase.Pass1Start  => "=== Pass 1: computing finish order ===",
            AlgoPhase.Pass1Visit  => Extra >= 0
                                     ? $"  Visit v{Vertex} (from v{Extra})"
                                     : $"  Start DFS from v{Vertex}",
            AlgoPhase.Pass1Finish => $"  Finish v{Vertex}",
            AlgoPhase.Pass2Start  => "=== Pass 2: assigning SCCs ===",
            AlgoPhase.Pass2Assign => $"  v{Vertex} → SCC {Extra}",
            AlgoPhase.Done        => "=== Done ===",
            _                     => $"{Phase} v{Vertex}"
        };
    }
}
