using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using Dxf;
using ReactiveUI;

namespace dxfInspect.ViewModels;

public class DxfViewerViewModel : ReactiveObject
{
    private readonly HierarchicalTreeDataGridSource<DxfTreeNodeViewModel> _source;
    private readonly HashSet<string> _expandedNodes = [];
    private bool _cellSelection;
    private string _codeSearch = "";
    private string _dataSearch = "";
    private int _lineNumberStart = 1;
    private int _lineNumberEnd = int.MaxValue;
    private List<DxfTreeNodeViewModel> _allNodes = [];
    private bool _hasLoadedFile;
    private bool _expandAll;
    private readonly ICommand _filterByLineRangeCommand;
    private readonly ICommand _filterByDataCommand;
    private readonly ICommand _filterByCodeCommand;
    private readonly ICommand _resetFiltersCommand;
    private readonly ICommand _copyCodeAndDataCommand;
    private readonly ICommand _copyObjectTreeCommand;
    private int _maxLineNumber = int.MaxValue;

    public DxfViewerViewModel()
    {
        _source = new HierarchicalTreeDataGridSource<DxfTreeNodeViewModel>(Array.Empty<DxfTreeNodeViewModel>())
        {
            Columns =
            {
                new HierarchicalExpanderColumn<DxfTreeNodeViewModel>(
                    new TextColumn<DxfTreeNodeViewModel, string>("Lines", x => x.LineNumberRange, new GridLength(200)),
                    x => x.Children,
                    x => x.HasChildren,
                    x => x.IsExpanded),
                new TextColumn<DxfTreeNodeViewModel, string>("Code", x => x.Code, new GridLength(100)),
                new TextColumn<DxfTreeNodeViewModel, string>("Data", x => x.Data, new GridLength(1, GridUnitType.Star))
            }
        };

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
        _filterByDataCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByData);
        _filterByCodeCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByCode);
        _resetFiltersCommand = ReactiveCommand.Create(ResetFilters);
        _copyCodeAndDataCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(CopyCodeAndData);
        _copyObjectTreeCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(CopyObjectTree);
    }

    public ICommand FilterByLineRangeCommand => _filterByLineRangeCommand;
    public ICommand FilterByDataCommand => _filterByDataCommand;
    public ICommand FilterByCodeCommand => _filterByCodeCommand;
    public ICommand ResetFiltersCommand => _resetFiltersCommand;
    public ICommand CopyCodeAndDataCommand => _copyCodeAndDataCommand;
    public ICommand CopyObjectTreeCommand => _copyObjectTreeCommand;

    public string CodeSearch
    {
        get => _codeSearch;
        set
        {
            this.RaiseAndSetIfChanged(ref _codeSearch, value);
            ApplyFilters();
        }
    }

    public string DataSearch
    {
        get => _dataSearch;
        set
        {
            this.RaiseAndSetIfChanged(ref _dataSearch, value);
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
        Console.WriteLine($"Loading DXF data. Number of sections: {sections.Count}");

        _allNodes = ConvertToTreeNodes(sections);
        Console.WriteLine($"Converted to tree nodes. Number of nodes: {_allNodes.Count}");

        if (_allNodes.Any())
        {
            var allNodes = _allNodes.SelectMany(GetAllNodes).ToList();
            _maxLineNumber = allNodes.Max(n => n.EndLine);
            Console.WriteLine($"Max line number: {_maxLineNumber}");
            Console.WriteLine($"Total nodes including children: {allNodes.Count}");

            LineNumberEnd = _maxLineNumber;
            LineNumberStart = 1;
        }
        else
        {
            Console.WriteLine("No nodes were created from sections");
        }

        HasLoadedFile = true;
        ApplyFilters();
    }

    private void ResetFilters()
    {
        CodeSearch = "";
        DataSearch = "";
        LineNumberStart = 1;
        LineNumberEnd = _maxLineNumber;
    }

    private IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
            {
                MainWindow: { } window
            })
        {
            return window.Clipboard;
        }

        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime { MainView: { } mainView })
        {
            var visualRoot = mainView.GetVisualRoot();
            if (visualRoot is TopLevel topLevel)
            {
                return topLevel.Clipboard;
            }
        }

        return null;
    }

    private async void CopyCodeAndData(DxfTreeNodeViewModel? nodeView)
    {
        if (nodeView != null)
        {
            var clipboard = GetClipboard();
            if (clipboard != null)
            {
                var text = $"{nodeView.OriginalGroupCodeLine}{Environment.NewLine}{nodeView.OriginalDataLine}";
                await clipboard.SetTextAsync(text);
            }
        }
    }

    private async void CopyObjectTree(DxfTreeNodeViewModel? nodeView)
    {
        if (nodeView?.RawTag != null)
        {
            var clipboard = GetClipboard();
            if (clipboard != null)
            {
                var text = nodeView.RawTag.GetOriginalTreeText();
                await clipboard.SetTextAsync(text);
            }
        }
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
            CodeSearch = "";
            DataSearch = "";
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

    private void FilterByCode(DxfTreeNodeViewModel? nodeView)
    {
        if (nodeView != null)
        {
            CodeSearch = nodeView.Code;
            DataSearch = "";
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
            DataSearch = nodeView.Data;
            CodeSearch = "";
            if (LineNumberEnd == _maxLineNumber)
            {
                LineNumberEnd = _maxLineNumber;
            }
        }
    }


    private void ApplyFilters()
    {
        var filteredNodes = FilterNodes(_allNodes);
        Console.WriteLine(
            $"Applying filters. Original nodes: {_allNodes.Count}, Filtered nodes: {filteredNodes.Count}");
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
                    node.NodeKey,
                    node.OriginalGroupCodeLine,
                    node.OriginalDataLine,
                    node.RawTag) { IsExpanded = _expandAll || _expandedNodes.Contains(node.NodeKey) };

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
                                child.NodeKey,
                                child.OriginalGroupCodeLine,
                                child.OriginalDataLine,
                                child.RawTag) { IsExpanded = _expandAll || _expandedNodes.Contains(child.NodeKey) };

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
                child.NodeKey,
                child.OriginalGroupCodeLine,
                child.OriginalDataLine,
                child.RawTag) { IsExpanded = _expandAll || _expandedNodes.Contains(child.NodeKey) };

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
                                nodeView.EndLine <= (LineNumberEnd == 1 ? int.MaxValue : LineNumberEnd);

        bool matchesCode = string.IsNullOrWhiteSpace(CodeSearch) ||
                           nodeView.Code.Equals(CodeSearch, StringComparison.OrdinalIgnoreCase);

        bool matchesData = string.IsNullOrWhiteSpace(DataSearch) ||
                           nodeView.Data.Contains(DataSearch, StringComparison.OrdinalIgnoreCase);

        return matchesLineRange && matchesCode && matchesData;
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
        var lineNumber = 1;

        Console.WriteLine($"Starting conversion of {sections.Count} sections");
        foreach (var section in sections)
        {
            Console.WriteLine(
                $"Section IsEnabled: {section.IsEnabled}, GroupCode: {section.GroupCode}, DataElement: {section.DataElement}");

            if (!section.IsEnabled)
            {
                Console.WriteLine("Skipping disabled section");
                continue;
            }

            var sectionNode = new DxfTreeNodeViewModel(
                lineNumber,
                lineNumber + 1,
                section.GroupCode.ToString(),
                section.DataElement ?? string.Empty,
                "SECTION",
                $"{lineNumber}:SECTION:{section.DataElement}",
                section.OriginalGroupCodeLine,
                section.OriginalDataLine,
                section);

            lineNumber += 2;

            if (section.Children != null)
            {
                Console.WriteLine($"Processing {section.Children.Count} children for section");
                AddChildNodes(sectionNode, section.Children, ref lineNumber);
                Console.WriteLine($"Added {sectionNode.Children.Count} children to node");
            }

            nodes.Add(sectionNode);
        }

        Console.WriteLine($"Converted {nodes.Count} nodes");
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
                $"{lineNumber}:{type}:{child.DataElement}",
                child.OriginalGroupCodeLine,
                child.OriginalDataLine,
                child);

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
