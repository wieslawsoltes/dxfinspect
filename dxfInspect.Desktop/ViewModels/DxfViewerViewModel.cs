using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
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
    private ICommand? _filterByLineRangeCommand;
    private ICommand? _filterByTypeCommand;
    private ICommand? _filterByDataCommand;
    private ICommand? _resetFiltersCommand;
    private int _maxLineNumber = int.MaxValue;

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
                    x => x.IsExpanded), // Bind to the model's IsExpanded property
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

        _filterByLineRangeCommand = ReactiveCommand.Create<DxfTreeNodeModel>(FilterByLineRange);
        _filterByTypeCommand = ReactiveCommand.Create<DxfTreeNodeModel>(FilterByType);
        _filterByDataCommand = ReactiveCommand.Create<DxfTreeNodeModel>(FilterByData);
        _resetFiltersCommand = ReactiveCommand.Create(ResetFilters);
    }

    public ICommand FilterByLineRangeCommand => _filterByLineRangeCommand!;
    public ICommand FilterByTypeCommand => _filterByTypeCommand!;
    public ICommand FilterByDataCommand => _filterByDataCommand!;
    public ICommand ResetFiltersCommand => _resetFiltersCommand!;

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
                _expandedNodes.Clear(); // Clear individual tracking when using ExpandAll
                foreach (var node in _allNodes)
                {
                    ExpandAllNodes(new List<DxfTreeNodeModel> { node });
                }
            }
            else
            {
                CollapseAllNodes(_allNodes);
            }
        }
    }

    public ITreeDataGridSource<DxfTreeNodeModel> Source => _source;

    public void LoadDxfData(IList<DxfRawTag> sections)
    {
        _allNodes = ConvertToTreeNodes(sections);

        if (_allNodes.Any())
        {
            _maxLineNumber = _allNodes
                .SelectMany(node => GetAllNodes(node))
                .Max(n => n.EndLine);

            LineNumberEnd = _maxLineNumber;
            LineNumberStart = 0;
        }

        HasLoadedFile = true;
        ApplyFilters();
    }

    private void ResetFilters()
    {
        SearchText = "";
        TypeFilter = "";
        LineNumberStart = 0;
        LineNumberEnd = _maxLineNumber;
    }

    private void FilterByLineRange(DxfTreeNodeModel node)
    {
        if (node != null)
        {
            // Get the full range including all children
            var startLine = node.StartLine;
            var endLine = node.EndLine;

            // If has children, recursively find min start line and max end line
            if (node.HasChildren)
            {
                var allLines = GetAllLineRanges(node);
                startLine = allLines.Min(r => r.StartLine);
                endLine = allLines.Max(r => r.EndLine);
            }

            LineNumberStart = startLine;
            LineNumberEnd = endLine;
        
            // Clear other filters
            SearchText = "";
            TypeFilter = "";
        }
    }

    private IEnumerable<DxfTreeNodeModel> GetAllLineRanges(DxfTreeNodeModel node)
    {
        yield return node;
    
        if (node.HasChildren)
        {
            foreach (var child in node.Children)
            {
                foreach (var descendant in GetAllLineRanges(child))
                {
                    yield return descendant;
                }
            }
        }
    }

    private void FilterByType(DxfTreeNodeModel node)
    {
        if (node != null)
        {
            TypeFilter = node.Type;
            SearchText = "";
            // Don't reset line range to int.MaxValue
            if (LineNumberEnd == _maxLineNumber)
            {
                LineNumberEnd = _maxLineNumber;
            }
        }
    }

    private void FilterByData(DxfTreeNodeModel node)
    {
        if (node != null)
        {
            SearchText = node.Data;
            TypeFilter = "";
            // Don't reset line range to int.MaxValue
            if (LineNumberEnd == _maxLineNumber)
            {
                LineNumberEnd = _maxLineNumber;
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
            bool isTypeNode = node.Code == DxfParser.DxfCodeForType.ToString();
            bool nodeMatches = MatchesFilters(node);
            bool hasMatchingDescendant = node.HasChildren && HasMatchingDescendant(node.Children.ToList());

            if (nodeMatches || hasMatchingDescendant)
            {
                var filteredNode = new DxfTreeNodeModel(
                    node.StartLine,
                    node.EndLine,
                    node.Code,
                    node.Data,
                    node.Type,
                    node.NodeKey) { IsExpanded = _expandAll || _expandedNodes.Contains(node.NodeKey) };

                if (node.HasChildren)
                {
                    if (isTypeNode && nodeMatches)
                    {
                        // For matching type nodes, include all children
                        foreach (var child in node.Children)
                        {
                            var childNode = new DxfTreeNodeModel(
                                child.StartLine,
                                child.EndLine,
                                child.Code,
                                child.Data,
                                child.Type,
                                child.NodeKey) { IsExpanded = _expandAll || _expandedNodes.Contains(child.NodeKey) };

                            if (child.HasChildren)
                            {
                                AddAllDescendants(child, childNode);
                            }

                            filteredNode.Children.Add(childNode);
                        }
                    }
                    else
                    {
                        // Normal filtering for non-type nodes or non-matching type nodes
                        var filteredChildren = FilterNodes(node.Children.ToList());
                        foreach (var child in filteredChildren)
                        {
                            filteredNode.Children.Add(child);
                        }
                    }
                }

                result.Add(filteredNode);
            }
        }

        return result;
    }

    private void AddAllDescendants(DxfTreeNodeModel source, DxfTreeNodeModel target)
    {
        foreach (var child in source.Children)
        {
            var childNode = new DxfTreeNodeModel(
                child.StartLine,
                child.EndLine,
                child.Code,
                child.Data,
                child.Type,
                child.NodeKey) { IsExpanded = _expandAll || _expandedNodes.Contains(child.NodeKey) };

            if (child.HasChildren)
            {
                AddAllDescendants(child, childNode);
            }

            target.Children.Add(childNode);
        }
    }

    private bool MatchesFilters(DxfTreeNodeModel node)
    {
        bool matchesLineRange = node.StartLine >= LineNumberStart &&
                                node.EndLine <= (LineNumberEnd == 0 ? int.MaxValue : LineNumberEnd);

        bool matchesType = string.IsNullOrWhiteSpace(TypeFilter) ||
                           node.Type.Contains(TypeFilter,
                               StringComparison.OrdinalIgnoreCase); // Changed to partial matching

        bool matchesSearch = string.IsNullOrWhiteSpace(SearchText) ||
                             node.Data.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                             node.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

        return matchesLineRange && matchesType && matchesSearch;
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
            string type = child.GroupCode == DxfParser.DxfCodeForType
                ? child.DataElement ?? "TYPE"
                : child.GroupCode.ToString();

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

    public void ExpandAllNodes(List<DxfTreeNodeModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.HasChildren)
            {
                node.IsExpanded = true;
                if (!_expandAll) // Only track individual nodes when not in ExpandAll mode
                {
                    _expandedNodes.Add(node.NodeKey);
                }

                ExpandAllNodes(node.Children.ToList());
            }
        }

        ApplyFilters(); // Refresh the view
    }

    public void CollapseAllNodes(List<DxfTreeNodeModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.HasChildren)
            {
                node.IsExpanded = false;
                CollapseAllNodes(node.Children.ToList());
            }
        }

        _expandedNodes.Clear();
        ApplyFilters(); // Refresh the view
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
