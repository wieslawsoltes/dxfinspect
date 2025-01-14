using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    private bool _isExpanding;
    private bool _isCollapsing;
    private readonly HashSet<string> _expandedNodes = [];
    private string _codeSearch = "";
    private string _dataSearch = "";
    private int _lineNumberStart = 1;
    private int _lineNumberEnd = int.MaxValue;
    private List<DxfTreeNodeViewModel> _allNodes = [];
    private bool _hasLoadedFile;
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

        ExpandAllCommand = ReactiveCommand.CreateFromTask(ExpandAllAsync);
        CollapseAllCommand = ReactiveCommand.CreateFromTask(CollapseAllAsync);
        FilterByLineRangeCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByLineRange);
        FilterByDataCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByData);
        FilterByCodeCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByCode);
        ResetFiltersCommand = ReactiveCommand.Create(ResetFilters);
        CopyCodeAndDataCommand = ReactiveCommand.CreateFromTask<DxfTreeNodeViewModel>(CopyCodeAndData);
        CopyObjectTreeCommand = ReactiveCommand.CreateFromTask<DxfTreeNodeViewModel>(CopyObjectTree);
    }

    public ICommand ExpandAllCommand { get; }

    public ICommand CollapseAllCommand { get; }
    
    public ICommand FilterByLineRangeCommand { get; }

    public ICommand FilterByDataCommand { get; }

    public ICommand FilterByCodeCommand { get; }

    public ICommand ResetFiltersCommand { get; }

    public ICommand CopyCodeAndDataCommand { get; }

    public ICommand CopyObjectTreeCommand { get; }

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

    public ITreeDataGridSource<DxfTreeNodeViewModel> Source => _source;

    public void LoadDxfData(IList<DxfRawTag> sections)
    {
        _expandedNodes.Clear();

        _allNodes = ConvertToTreeNodes(sections);

        if (_allNodes.Any())
        {
            var allNodes = _allNodes.SelectMany(GetAllNodes).ToList();
            _maxLineNumber = allNodes.Max(n => n.EndLine);
            LineNumberEnd = _maxLineNumber;
            LineNumberStart = 1;
        }

        HasLoadedFile = true;
        ApplyFilters();
    }
    
    private async Task ExpandAllAsync(CancellationToken cancellationToken)
    {
        if (_isExpanding)
        {
            return;
        }

        try
        {
            _isExpanding = true;

            await Task.Run(() =>
            {
                _expandedNodes.Clear();
                foreach (var node in _allNodes)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    ExpandAllNodes([node]);
                }
            }, cancellationToken);

            ApplyFilters();
        }
        finally
        {
            _isExpanding = false;
        }
    }
    
    private async Task CollapseAllAsync(CancellationToken cancellationToken)
    {
        if (_isCollapsing)
        {
            return;
        }

        try
        {
            _isCollapsing = true;

            await Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                CollapseAllNodes(_allNodes);
            }, cancellationToken);

            ApplyFilters();
        }
        finally
        {
            _isCollapsing = false;
        }
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

    private async Task CopyCodeAndData(DxfTreeNodeViewModel? nodeView)
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

    private async Task CopyObjectTree(DxfTreeNodeViewModel? nodeView)
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
            var startLine = nodeView.StartLine;
            var endLine = nodeView.EndLine;

            if (nodeView.HasChildren)
            {
                var allLines = GetAllLineRanges(nodeView).ToList();
                startLine = allLines.Min(r => r.StartLine);
                endLine = allLines.Max(r => r.EndLine);
            }

            LineNumberStart = startLine;
            LineNumberEnd = endLine;

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
                    node.RawTag) { IsExpanded = _expandedNodes.Contains(node.NodeKey) };

                if (node.HasChildren)
                {
                    if (isTypeNode && nodeMatches)
                    {
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
                                child.RawTag) { IsExpanded = _expandedNodes.Contains(child.NodeKey) };

                            if (child.HasChildren)
                            {
                                AddAllDescendants(child, childNode);
                            }

                            filteredNode.Children.Add(childNode);
                        }
                    }
                    else
                    {
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
                child.RawTag) { IsExpanded = _expandedNodes.Contains(child.NodeKey) };

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

        foreach (var section in sections)
        {
            if (!section.IsEnabled)
            {
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
                _expandedNodes.Add(node.NodeKey);
                ExpandAllNodes(node.Children.ToList());
            }
        }
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
