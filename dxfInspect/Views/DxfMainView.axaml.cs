using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;
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
                    await using var stream = await file.OpenReadAsync();
                
                    var parseStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var sections = await Task.Run(() => DxfParser.ParseStream(stream));
                    parseStopwatch.Stop();
                    Console.WriteLine($"ParseStreamAsync for {file.Name} took {parseStopwatch.ElapsedMilliseconds}ms");

                    var addTabStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    _viewModel.AddNewFileTab(sections, file.Name);
                    addTabStopwatch.Stop();
                    Console.WriteLine($"AddNewFileTab for {file.Name} took {addTabStopwatch.ElapsedMilliseconds}ms");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }
}
