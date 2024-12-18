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
    private readonly HashSet<string> _expandedNodes = new();
    private bool _cellSelection;
    private string _searchText = "";
    private string _typeFilter = "";
    private int _lineNumberStart;
    private int _lineNumberEnd = int.MaxValue;
    private List<DxfTreeNodeModel> _allNodes = new();
    private bool _hasLoadedFile;
    private bool _expandAll;

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
                    x => x.IsExpanded),  // Bind to the model's IsExpanded property
                new TextColumn<DxfTreeNodeModel, string>("Code", x => x.Code, new GridLength(100)),
                new TextColumn<DxfTreeNodeModel, string>("Type", x => x.Type, new GridLength(150)),
                new TextColumn<DxfTreeNodeModel, string>("Data", x => x.Data, new GridLength(1, GridUnitType.Star))
            }
        };

        // Subscribe to model changes to track expanded state
        _source.RowExpanded += (s, e) =>
        {
            if (e.Row.Model is DxfTreeNodeModel model)
            {
                _expandedNodes.Add(model.NodeKey);
            }
        };

        _source.RowCollapsed += (s, e) =>
        {
            if (e.Row.Model is DxfTreeNodeModel model)
            {
                _expandedNodes.Remove(model.NodeKey);
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

    public bool ExpandAll
    {
        get => _expandAll;
        set
        {
            this.RaiseAndSetIfChanged(ref _expandAll, value);
            if (value)
            {
                ExpandAllNodes(_allNodes);
            }
            else
            {
                CollapseAllNodes(_allNodes);
            }
            ApplyFilters();
        }
    }

    public ITreeDataGridSource<DxfTreeNodeModel> Source => _source;

    public void LoadDxfData(IList<DxfRawTag> sections)
    {
        _allNodes = ConvertToTreeNodes(sections);
        
        if (_allNodes.Any())
        {
            var maxLine = _allNodes
                .SelectMany(node => GetAllNodes(node))
                .Max(n => n.EndLine);
            
            LineNumberEnd = maxLine;
            LineNumberStart = 0;
        }
        
        HasLoadedFile = true;
        ApplyFilters();
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
            bool nodeOrDescendantMatches = MatchesFilters(node) || 
                (node.HasChildren && HasMatchingDescendant(node.Children.ToList()));

            if (nodeOrDescendantMatches)
            {
                var filteredNode = new DxfTreeNodeModel(
                    node.StartLine,
                    node.EndLine,
                    node.Code,
                    node.Data,
                    node.Type,
                    GetNodeKey(node));

                if (node.HasChildren)
                {
                    var filteredChildren = FilterNodes(node.Children.ToList());
                    foreach (var child in filteredChildren)
                    {
                        filteredNode.Children.Add(child);
                    }
                }

                result.Add(filteredNode);
            }
        }
        return result;
    }

    private bool HasMatchingDescendant(List<DxfTreeNodeModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (MatchesFilters(node)) return true;
            if (node.HasChildren && HasMatchingDescendant(node.Children.ToList())) return true;
        }
        return false;
    }

    private bool MatchesFilters(DxfTreeNodeModel node)
    {
        bool matchesLineRange = node.StartLine >= LineNumberStart && 
                              node.EndLine <= (LineNumberEnd == 0 ? int.MaxValue : LineNumberEnd);

        bool matchesType = string.IsNullOrWhiteSpace(TypeFilter) || 
                          node.Type.Equals(TypeFilter, StringComparison.OrdinalIgnoreCase);

        bool matchesSearch = string.IsNullOrWhiteSpace(SearchText) || 
                           node.Data.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                           node.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

        return matchesLineRange && matchesType && matchesSearch;
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
                "SECTION",
                $"{lineNumber}:SECTION:{section.DataElement}");

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
                type,
                $"{lineNumber}:{type}:{child.DataElement}");

            lineNumber += 2;

            if (child.Children != null)
            {
                AddChildNodes(node, child.Children, ref lineNumber);
            }

            parent.Children.Add(node);
        }
    }

    private void ExpandAllNodes(List<DxfTreeNodeModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.HasChildren)
            {
                _expandedNodes.Add(node.NodeKey);
                ExpandAllNodes(node.Children.ToList());
            }
        }
    }

    private void CollapseAllNodes(List<DxfTreeNodeModel> nodes)
    {
        _expandedNodes.Clear();
    }

    private static string GetNodeKey(DxfTreeNodeModel node)
    {
        return node.NodeKey;
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
}