using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dxf;
using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Platform.Storage;

namespace dxfInspect.Desktop;

public partial class MainWindow : Window
{
    private TextBlock? fileNameBlock;
    private DataGrid? dxfGrid;
    private TextBlock? placeholderText;

    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif

        var loadButton = this.FindControl<Button>("LoadButton");
        fileNameBlock = this.FindControl<TextBlock>("FileNameBlock");
        dxfGrid = this.FindControl<DataGrid>("DxfGrid");
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
                
                if (dxfGrid != null)
                {
                    var rows = ConvertToRows(sections);
                    var collectionView = new DataGridCollectionView(rows);
                    collectionView.GroupDescriptions.Add(new DataGridPathGroupDescription("SectionName"));
                    dxfGrid.ItemsSource = collectionView;
                    dxfGrid.IsVisible = true;
                    
                    if (placeholderText != null)
                    {
                        placeholderText.IsVisible = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static List<DxfRow> ConvertToRows(IList<DxfRawTag> sections)
    {
        var rows = new List<DxfRow>();
        var lineNumber = 0;

        foreach (var section in sections.Where(s => s.IsEnabled))
        {
            // Get section name from first NAME child if available
            string sectionName = section.Children?.Count > 0 && 
                               section.Children[0].GroupCode == DxfParser.DxfCodeForName
                ? section.Children[0].DataElement
                : section.DataElement;

            // Add section header
            rows.Add(new DxfRow
            {
                LineNumber = lineNumber += 2,
                Code = section.GroupCode.ToString(),
                Data = section.DataElement,
                RowType = "Section",
                SectionName = $"SECTION: {sectionName}"
            });

            if (section.Children != null)
            {
                var currentGroup = $"SECTION: {sectionName}";
                var currentEntityType = "";

                foreach (var child in section.Children.Where(c => c.IsEnabled))
                {
                    if (child.GroupCode == DxfParser.DxfCodeForType)
                    {
                        currentEntityType = child.DataElement;
                        currentGroup = $"SECTION: {sectionName} | {currentEntityType}";
                        
                        // Entity type header
                        rows.Add(new DxfRow
                        {
                            LineNumber = lineNumber += 2,
                            Code = child.GroupCode.ToString(),
                            Data = child.DataElement,
                            RowType = "Other",
                            SectionName = currentGroup
                        });

                        if (child.Children != null)
                        {
                            foreach (var entity in child.Children.Where(e => e.IsEnabled))
                            {
                                rows.Add(new DxfRow
                                {
                                    LineNumber = lineNumber += 2,
                                    Code = entity.GroupCode.ToString(),
                                    Data = entity.DataElement,
                                    RowType = "Row",
                                    SectionName = currentGroup
                                });
                            }
                        }
                    }
                    else
                    {
                        // If not a type code, add to current group
                        rows.Add(new DxfRow
                        {
                            LineNumber = lineNumber += 2,
                            Code = child.GroupCode.ToString(),
                            Data = child.DataElement,
                            RowType = "Row",
                            SectionName = currentGroup
                        });
                    }
                }
            }
        }

        return rows;
    }
}
