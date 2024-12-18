using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Dxf;
using ReactiveUI;

namespace dxfInspect.ViewModels;

public class DxfViewerViewModel : ReactiveObject
{
    private readonly HierarchicalTreeDataGridSource<DxfTreeNodeViewModel> _source;
    private readonly HashSet<string> _expandedNodes = [];
    private bool _cellSelection;
    private string _searchText = "";
    private string _typeFilter = "";
    private int _lineNumberStart;
    private int _lineNumberEnd = int.MaxValue;
    private List<DxfTreeNodeViewModel> _allNodes = [];
    private bool _hasLoadedFile;
    private bool _expandAll;
    private readonly ICommand? _filterByLineRangeCommand;
    private readonly ICommand? _filterByTypeCommand;
    private readonly ICommand? _filterByDataCommand;
    private readonly ICommand? _resetFiltersCommand;
    private int _maxLineNumber = int.MaxValue;

    public DxfViewerViewModel()
    {
        _source = new HierarchicalTreeDataGridSource<DxfTreeNodeViewModel>(Array.Empty<DxfTreeNodeViewModel>())
        {
            Columns =
            {
                new HierarchicalExpanderColumn<DxfTreeNodeViewModel>(
                    new TextColumn<DxfTreeNodeViewModel, string>("Lines", x => x.LineNumberRange, new GridLength(100)),
                    x => x.Children,
                    x => x.HasChildren,
                    x => x.IsExpanded), // Bind to the model's IsExpanded property
                new TextColumn<DxfTreeNodeViewModel, string>("Code", x => x.Code, new GridLength(100)),
                new TextColumn<DxfTreeNodeViewModel, string>("Type", x => x.Type, new GridLength(150)),
                new TextColumn<DxfTreeNodeViewModel, string>("Data", x => x.Data, new GridLength(1, GridUnitType.Star))
            }
        };

        // Subscribe to model changes to track expanded state
        _source.RowExpanded += (_, e) =>
        {
            if (e.Row.Model is { } model)
            {
                _expandedNodes.Add(model.NodeKey);
            }
        };

        _source.RowCollapsed += (_, e) =>
        {
            if (e.Row.Model is { } model)
            {
                _expandedNodes.Remove(model.NodeKey);
            }
        };

        _filterByLineRangeCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByLineRange);
        _filterByTypeCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByType);
        _filterByDataCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByData);
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
                    Source.Selection = new TreeDataGridCellSelectionModel<DxfTreeNodeViewModel>(Source);
                else
                    Source.Selection = new TreeDataGridRowSelectionModel<DxfTreeNodeViewModel>(Source);
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
                    ExpandAllNodes([node]);
                }
            }
            else
            {
                CollapseAllNodes(_allNodes);
            }
        }
    }

    public ITreeDataGridSource<DxfTreeNodeViewModel> Source => _source;

    public void LoadDxfData(IList<DxfRawTag> sections)
    {
        _allNodes = ConvertToTreeNodes(sections);

        if (_allNodes.Any())
        {
            _maxLineNumber = _allNodes
                .SelectMany(GetAllNodes)
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

    private void FilterByLineRange(DxfTreeNodeViewModel? nodeView)
    {
        if (nodeView != null)
        {
            // Get the full range including all children
            var startLine = nodeView.StartLine;
            var endLine = nodeView.EndLine;

            // If it has children, recursively find min start line and max end line
            if (nodeView.HasChildren)
            {
                var allLines = GetAllLineRanges(nodeView).ToList();
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

    private IEnumerable<DxfTreeNodeViewModel> GetAllLineRanges(DxfTreeNodeViewModel nodeView)
    {
        yield return nodeView;

        if (nodeView.HasChildren)
        {
            foreach (var child in nodeView.Children)
            {
                foreach (var descendant in GetAllLineRanges(child))
                {
                    yield return descendant;
                }
            }
        }
    }

    private void FilterByType(DxfTreeNodeViewModel? nodeView)
    {
        if (nodeView != null)
        {
            TypeFilter = nodeView.Type;
            SearchText = "";
            // Don't reset line range to int.MaxValue
            if (LineNumberEnd == _maxLineNumber)
            {
                LineNumberEnd = _maxLineNumber;
            }
        }
    }

    private void FilterByData(DxfTreeNodeViewModel? nodeView)
    {
        if (nodeView != null)
        {
            SearchText = nodeView.Data;
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

    private List<DxfTreeNodeViewModel> FilterNodes(List<DxfTreeNodeViewModel> nodes)
    {
        var result = new List<DxfTreeNodeViewModel>();
        foreach (var node in nodes)
        {
            bool isTypeNode = node.Code == DxfParser.DxfCodeForType.ToString();
            bool nodeMatches = MatchesFilters(node);
            bool hasMatchingDescendant = node.HasChildren && HasMatchingDescendant(node.Children.ToList());

            if (nodeMatches || hasMatchingDescendant)
            {
                var filteredNode = new DxfTreeNodeViewModel(
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
                            var childNode = new DxfTreeNodeViewModel(
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

    private void AddAllDescendants(DxfTreeNodeViewModel source, DxfTreeNodeViewModel target)
    {
        foreach (var child in source.Children)
        {
            var childNode = new DxfTreeNodeViewModel(
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

    private bool MatchesFilters(DxfTreeNodeViewModel nodeView)
    {
        bool matchesLineRange = nodeView.StartLine >= LineNumberStart &&
                                nodeView.EndLine <= (LineNumberEnd == 0 ? int.MaxValue : LineNumberEnd);

        bool matchesType = string.IsNullOrWhiteSpace(TypeFilter) ||
                           nodeView.Type.Contains(TypeFilter,
                               StringComparison.OrdinalIgnoreCase); // Changed to partial matching

        bool matchesSearch = string.IsNullOrWhiteSpace(SearchText) ||
                             nodeView.Data.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                             nodeView.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

        return matchesLineRange && matchesType && matchesSearch;
    }

    private bool HasMatchingDescendant(List<DxfTreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (MatchesFilters(node)) return true;
            if (node.HasChildren && HasMatchingDescendant(node.Children.ToList())) return true;
        }

        return false;
    }

    private static List<DxfTreeNodeViewModel> ConvertToTreeNodes(IList<DxfRawTag> sections)
    {
        var nodes = new List<DxfTreeNodeViewModel>();
        var lineNumber = 0;

        foreach (var section in sections.Where(s => s.IsEnabled))
        {
            var sectionNode = new DxfTreeNodeViewModel(
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

    private static void AddChildNodes(DxfTreeNodeViewModel parent, IList<DxfRawTag> children, ref int lineNumber)
    {
        foreach (var child in children.Where(c => c.IsEnabled))
        {
            string type = child.GroupCode == DxfParser.DxfCodeForType
                ? child.DataElement ?? "TYPE"
                : child.GroupCode.ToString();

            var node = new DxfTreeNodeViewModel(
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

    private void ExpandAllNodes(List<DxfTreeNodeViewModel> nodes)
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

    private void CollapseAllNodes(List<DxfTreeNodeViewModel> nodes)
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

    private IEnumerable<DxfTreeNodeViewModel> GetAllNodes(DxfTreeNodeViewModel nodeView)
    {
        yield return nodeView;
        foreach (var child in nodeView.Children)
        {
            foreach (var descendant in GetAllNodes(child))
            {
                yield return descendant;
            }
        }
    }
}
