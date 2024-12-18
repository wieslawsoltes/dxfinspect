using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Dxf;
using ReactiveUI;

namespace dxfInspect.Desktop;

public class DxfViewerViewModel : ReactiveObject
{
    private readonly HierarchicalTreeDataGridSource<DxfTreeNodeModel> _source;
    private bool _cellSelection;
    private string _searchText = "";
    private string _typeFilter = "";
    private int _lineNumberStart;
    private int _lineNumberEnd = int.MaxValue;
    private List<DxfTreeNodeModel> _allNodes = new();
    private bool _hasLoadedFile;

    public DxfViewerViewModel()
    {
        _source = new HierarchicalTreeDataGridSource<DxfTreeNodeModel>(Array.Empty<DxfTreeNodeModel>())
        {
            Columns =
            {
                new HierarchicalExpanderColumn<DxfTreeNodeModel>(
                    new TextColumn<DxfTreeNodeModel, string>("Lines", x => x.LineNumberRange, new GridLength(100)),
                    x => x.Children,
                    x => x.HasChildren,
                    x => x.IsExpanded),
                new TextColumn<DxfTreeNodeModel, string>("Code", x => x.Code, new GridLength(100)),
                new TextColumn<DxfTreeNodeModel, string>("Type", x => x.Type, new GridLength(150)),
                new TextColumn<DxfTreeNodeModel, string>("Data", x => x.Data, new GridLength(1, GridUnitType.Star))
            }
        };
    }

    public bool CellSelection
    {
        get => _cellSelection;
        set
        {
            if (_cellSelection != value)
            {
                _cellSelection = value;
                if (_cellSelection)
                    Source.Selection = new TreeDataGridCellSelectionModel<DxfTreeNodeModel>(Source);
                else
                    Source.Selection = new TreeDataGridRowSelectionModel<DxfTreeNodeModel>(Source);
                this.RaisePropertyChanged();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            ApplyFilters();
        }
    }

    public string TypeFilter
    {
        get => _typeFilter;
        set
        {
            this.RaiseAndSetIfChanged(ref _typeFilter, value);
            ApplyFilters();
        }
    }

    public int LineNumberStart
    {
        get => _lineNumberStart;
        set
        {
            this.RaiseAndSetIfChanged(ref _lineNumberStart, value);
            ApplyFilters();
        }
    }

    public int LineNumberEnd
    {
        get => _lineNumberEnd;
        set
        {
            this.RaiseAndSetIfChanged(ref _lineNumberEnd, value);
            ApplyFilters();
        }
    }
    
    public bool HasLoadedFile
    {
        get => _hasLoadedFile;
        private set => this.RaiseAndSetIfChanged(ref _hasLoadedFile, value);
    }
    
    public ITreeDataGridSource<DxfTreeNodeModel> Source => _source;

    public void LoadDxfData(IList<DxfRawTag> sections)
    {
        _allNodes = ConvertToTreeNodes(sections);
        
        // Update the line range to match the file content
        if (_allNodes.Any())
        {
            var maxLine = _allNodes
                .SelectMany(node => GetAllNodes(node))
                .Max(n => n.EndLine);
            
            LineNumberStart = 0;
            LineNumberEnd = maxLine;
        }
        
        HasLoadedFile = true;
        ApplyFilters();
    }
    
    private IEnumerable<DxfTreeNodeModel> GetAllNodes(DxfTreeNodeModel node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var descendant in GetAllNodes(child))
            {
                yield return descendant;
            }
        }
    }
    
    private void ApplyFilters()
    {
        var filteredNodes = FilterNodes(_allNodes);
        _source.Items = filteredNodes;
    }

    private List<DxfTreeNodeModel> FilterNodes(List<DxfTreeNodeModel> nodes)
    {
        var result = new List<DxfTreeNodeModel>();
        foreach (var node in nodes)
        {
            if (MatchesFilters(node))
            {
                var filteredNode = new DxfTreeNodeModel(
                    node.StartLine,
                    node.EndLine,
                    node.Code,
                    node.Data,
                    node.Type)
                {
                    IsExpanded = node.IsExpanded
                };

                if (node.HasChildren)
                {
                    var filteredChildren = FilterNodes(node.Children.ToList());
                    foreach (var child in filteredChildren)
                    {
                        filteredNode.Children.Add(child);
                    }
                }

                if (filteredNode.HasChildren || MatchesFilters(node))
                {
                    result.Add(filteredNode);
                }
            }
        }
        return result;
    }

    private bool MatchesFilters(DxfTreeNodeModel node)
    {
        return node.StartLine >= LineNumberStart &&
               node.EndLine <= LineNumberEnd &&
               (string.IsNullOrWhiteSpace(TypeFilter) || 
                node.Type.Contains(TypeFilter, StringComparison.OrdinalIgnoreCase)) &&
               (string.IsNullOrWhiteSpace(SearchText) || 
                node.Data.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
    }

    private static List<DxfTreeNodeModel> ConvertToTreeNodes(IList<DxfRawTag> sections)
    {
        var nodes = new List<DxfTreeNodeModel>();
        var lineNumber = 0;

        foreach (var section in sections.Where(s => s.IsEnabled))
        {
            var sectionNode = new DxfTreeNodeModel(
                lineNumber,
                lineNumber + 1,
                section.GroupCode.ToString(),
                section.DataElement ?? string.Empty,
                "SECTION");

            lineNumber += 2;

            if (section.Children != null)
            {
                AddChildNodes(sectionNode, section.Children, ref lineNumber);
            }

            nodes.Add(sectionNode);
        }

        return nodes;
    }

    private static void AddChildNodes(DxfTreeNodeModel parent, IList<DxfRawTag> children, ref int lineNumber)
    {
        foreach (var child in children.Where(c => c.IsEnabled))
        {
            string type = child.GroupCode == DxfParser.DxfCodeForType ? 
                child.DataElement ?? "TYPE" : 
                child.GroupCode.ToString();
            
            var node = new DxfTreeNodeModel(
                lineNumber,
                lineNumber + 1,
                child.GroupCode.ToString(),
                child.DataElement ?? string.Empty,
                type);

            lineNumber += 2;

            if (child.Children != null)
            {
                AddChildNodes(node, child.Children, ref lineNumber);
            }

            parent.Children.Add(node);
        }
    }
}
