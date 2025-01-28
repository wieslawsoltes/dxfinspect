using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using dxfInspect.Services;
using dxfInspect.ViewModels;

namespace dxfInspect.Views;

public partial class DxfMainView : UserControl
{
    private readonly MainViewModel _viewModel;

    public DxfMainView()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        var loadButton = this.FindControl<Button>("LoadButton");
        if (loadButton != null)
        {
            loadButton.Click += LoadButton_Click;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void LoadButton_Click(object? sender, RoutedEventArgs e)
    {
        var storageProvider = (this.GetVisualRoot() as TopLevel)?.StorageProvider;
        if (storageProvider is null)
        {
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select DXF File",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("DXF Files") { Patterns = ["*.dxf"] },
                new FilePickerFileType("All Files") { Patterns = ["*.*"] }
            ]
        });

        if (files.Count > 0)
        {
            foreach (var file in files)
            {
                await _viewModel.LoadDxfFileAsync(file);
            }
        }
    }

    private async void OnDragOver(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles()?.ToList();
        var hasDxf = files?.Any(f => f.Name.EndsWith(".dxf", System.StringComparison.OrdinalIgnoreCase)) ?? false;
    
        e.DragEffects = hasDxf 
            ? DragDropEffects.Copy 
            : DragDropEffects.None;
    
        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles()?
            .Where(f => f.Name.EndsWith(".dxf", System.StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (files?.Count > 0)
        {
            foreach (var file in files.Cast<IStorageFile>())
            {
                await _viewModel.LoadDxfFileAsync(file);
            }
        }
    }
}
