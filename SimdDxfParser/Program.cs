using System.Diagnostics;
using SimdDxfParser;

var path = "/Users/wieslawsoltes/Downloads/sample-files-master/dxf/dxf-parser/floorplan.dxf";
using var parser = new DxfParser();
//var parser = new DxfParser2();

var sw = Stopwatch.StartNew();
var tags = parser.ParseFile(path).ToList();
//var tags = parser.Parse(path);
sw.Stop();
Console.WriteLine($"{sw.Elapsed.TotalMilliseconds}ms");

/*
foreach (var (code, value) in parser.ParseFile(path))
{
    Console.WriteLine($"Code: {code}, Value: {value}");
}
*/
