using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using Dxf;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using dxfInspect.ViewModels;

namespace dxfInspect.Views;

public class DxfMainView : UserControl
{
    private readonly TextBlock? _fileNameBlock;
    private readonly DxfViewerViewModel _viewModel;

    public DxfMainView()
    {
        InitializeComponent();

        _viewModel = new DxfViewerViewModel();
        
        DataContext = _viewModel;

        var loadButton = this.FindControl<Button>("LoadButton");
        _fileNameBlock = this.FindControl<TextBlock>("FileNameBlock");
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
        try
        {
            var storageProvider = (this.GetVisualRoot() as TopLevel)?.StorageProvider;
            if (storageProvider is null)
            {
                return;
            }

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select DXF File",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("DXF Files") { Patterns = ["*.dxf"] },
                    new FilePickerFileType("All Files") { Patterns = ["*.*"] }
                ]
            });

            if (files.Count > 0)
            {
                var file = files[0];
                if (_fileNameBlock != null) 
                {
                    _fileNameBlock.Text = file.Name;
                }

                await using var stream = await file.OpenReadAsync();
                var text = await new StreamReader(stream).ReadToEndAsync();
                var sections = DxfParser.Parse(text);
                _viewModel.LoadDxfData(sections);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }
}

