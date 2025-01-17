using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace dxfInspect.Views;

public class DxfTreeView : UserControl
{
    public DxfTreeView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

