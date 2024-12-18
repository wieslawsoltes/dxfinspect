using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using Dxf;
using Avalonia.Platform.Storage;

namespace dxfInspect.Desktop;

public partial class MainWindow : Window
{
    private TextBlock? fileNameBlock;
    private TreeDataGrid? dxfTree;
    private TextBlock? placeholderText;
    private DxfViewerViewModel viewModel;

    public MainWindow()
    {
        InitializeComponent();
        viewModel = new DxfViewerViewModel();
        DataContext = viewModel;

#if DEBUG
        this.AttachDevTools();
#endif

        var loadButton = this.FindControl<Button>("LoadButton");
        fileNameBlock = this.FindControl<TextBlock>("FileNameBlock");
        dxfTree = this.FindControl<TreeDataGrid>("DxfTree");
        placeholderText = this.FindControl<TextBlock>("PlaceholderText");

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
            var storageProvider = StorageProvider;
            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select DXF File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("DXF Files") { Patterns = new[] { "*.dxf" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                if (fileNameBlock != null) 
                {
                    fileNameBlock.Text = file.Name;
                }

                var text = await File.ReadAllTextAsync(file.Path.LocalPath);
                var sections = DxfParser.Parse(text);
                viewModel.LoadDxfData(sections);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }
}
