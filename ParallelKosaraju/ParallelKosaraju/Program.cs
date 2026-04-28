using ParallelKosaraju;

class Program
{
    public static void Main(string[] args)
    {
        var degree = 5;
        if (args.Length > 0 && int.TryParse(args[0], out int parsed))
        {
            degree = parsed;
        }

        GraphHelper.Benchmark(degree);
    }
}