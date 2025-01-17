using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace dxfInspect.Views;

public class DxfMainWindow : Window
{
    public DxfMainWindow()
    {
        InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
