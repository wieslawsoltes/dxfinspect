
using Dxf;

if (args.Length == 1)
{
    Console.WriteLine(DxfInspect.ToHtml(args[0]));
}
else if (args.Length == 2)
{
    DxfInspect.Convert(args[0], args[1]);
}
