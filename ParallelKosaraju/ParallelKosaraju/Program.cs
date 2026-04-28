using ParallelKosaraju;

var degree = 5;

Console.WriteLine("Enter the degree of vertices:");
if (int.TryParse(Console.ReadLine(), out var parsedDegree))
{
    degree = parsedDegree;
}

GraphHelper.Benchmark(degree);