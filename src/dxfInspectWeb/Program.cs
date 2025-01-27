using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using dxfInspect;

[assembly:SupportedOSPlatform("browser")]

internal sealed class Program
{
    private static async Task Main(string[] args) 
        => await BuildAvaloniaApp().StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder
            .Configure<DxfApp>()
            .WithInterFont()
            .UseSkia();
}
