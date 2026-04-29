using ParallelKosaraju;
using System.Globalization;

var edgeRatio = 5d;

Console.Write("Enter the edge ratio to vertices: ");
if (double.TryParse(Console.ReadLine(), CultureInfo.InvariantCulture, out var parsedRatio))
{
    edgeRatio = parsedRatio;
}

GraphHelper.Benchmark(edgeRatio);