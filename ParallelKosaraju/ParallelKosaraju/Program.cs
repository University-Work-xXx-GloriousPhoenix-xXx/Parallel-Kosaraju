using ParallelKosaraju;
using System.Globalization;

var GLOBAL_RUNS = 10;
var edgeRatio = 5d;


//Console.Write("Enter the edge ratio to vertices: ");
//if (double.TryParse(Console.ReadLine(), CultureInfo.InvariantCulture, out var parsedRatio))
//{
//    edgeRatio = parsedRatio;
//}

for (var gr = 0; gr < GLOBAL_RUNS; gr++)
{
    var suffix = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
    GraphHelper.Benchmark(edgeRatio, suffix);
    Thread.Sleep(5);
}