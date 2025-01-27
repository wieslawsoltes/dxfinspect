using System.Diagnostics;

var path = "/Users/wieslawsoltes/Downloads/sample-files-master/dxf/dxf-parser/floorplan.dxf";
using var parser = new DxfParser();
var sw = Stopwatch.StartNew();
var tags = parser.ParseFile(path);
sw.Stop();
Console.WriteLine($"{sw.Elapsed.TotalMilliseconds}ms");
