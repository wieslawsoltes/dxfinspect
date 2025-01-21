using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using dxfInspect.Services;
using dxfInspect.Model;
using ReactiveUI;

namespace dxfInspect.ViewModels;

public class DxfTreeViewModel : ReactiveObject
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
    private string _fileName = "-";
    private ObservableCollection<TagModel> _codeTags;
    private ObservableCollection<TagModel> _dataTags;
    private string _newCodeTag = "";
    private string _newDataTag = "";

    public DxfTreeViewModel()
    {
        _source = new HierarchicalTreeDataGridSource<DxfTreeNodeViewModel>(Array.Empty<DxfTreeNodeViewModel>())
        {
            Columns =
            {
                new HierarchicalExpanderColumn<DxfTreeNodeViewModel>(
                    new TextColumn<DxfTreeNodeViewModel, DxfLineRange>(
                        "Lines",
                        x => x.LineRange,
                        options: new()
                        {
                            CompareAscending = Sort<DxfTreeNodeViewModel>.Ascending(x => x.LineRange),
                            CompareDescending = Sort<DxfTreeNodeViewModel>.Descending(x => x.LineRange)
                        },
                        width: new GridLength(200)),
                    x => x.Children,
                    x => x.HasChildren,
                    x => x.IsExpanded),
                new TextColumn<DxfTreeNodeViewModel, string>(
                    "Code",
                    x => x.CodeString,
                    options: new()
                    {
                        CompareAscending = Sort<DxfTreeNodeViewModel>.Ascending(x => x.Code),
                        CompareDescending = Sort<DxfTreeNodeViewModel>.Descending(x => x.Code)
                    },
                    width: new GridLength(80)),
                new TextColumn<DxfTreeNodeViewModel, string>(
                    "Data",
                    x => x.Data,
                    options: new()
                    {
                        CompareAscending = Sort<DxfTreeNodeViewModel>.Ascending(x => x.Data),
                        CompareDescending = Sort<DxfTreeNodeViewModel>.Descending(x => x.Data)
                    },
                    width: new GridLength(2, GridUnitType.Star)),
                new TextColumn<DxfTreeNodeViewModel, string>(
                    "Value Type",
                    x => x.GroupCodeValueType,
                    options: new()
                    {
                        CompareAscending = Sort<DxfTreeNodeViewModel>.Ascending(x => x.GroupCodeValueType),
                        CompareDescending = Sort<DxfTreeNodeViewModel>.Descending(x => x.GroupCodeValueType)
                    },
                    width: new GridLength(1, GridUnitType.Star)),
                new TextColumn<DxfTreeNodeViewModel, string>(
                    "Description",
                    x => x.GroupCodeDescription,
                    options: new()
                    {
                        CompareAscending = Sort<DxfTreeNodeViewModel>.Ascending(x => x.GroupCodeDescription),
                        CompareDescending = Sort<DxfTreeNodeViewModel>.Descending(x => x.GroupCodeDescription)
                    },
                    width: new GridLength(2, GridUnitType.Star)),
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
        
        _codeTags = new ObservableCollection<TagModel>();
        _dataTags = new ObservableCollection<TagModel>();

        ExpandAllCommand = ReactiveCommand.CreateFromTask(ExpandAllAsync);
        CollapseAllCommand = ReactiveCommand.CreateFromTask(CollapseAllAsync);
        FilterByLineRangeCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByLineRange);
        FilterByDataCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByData);
        FilterByCodeCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByCode);
        ResetFiltersCommand = ReactiveCommand.Create(ResetFilters);
        ResetCodeCommand = ReactiveCommand.Create(ResetCode);
        ResetDataCommand = ReactiveCommand.Create(ResetData);
        ReseLineNumberStartCommand = ReactiveCommand.Create(ResetLineNumberStart);
        ResetLineNumberEndCommand = ReactiveCommand.Create(ResetLineNumberEnd);
        ResetLineRangeCommand = ReactiveCommand.Create(ResetLineRange);
        CopyCodeAndDataCommand = ReactiveCommand.CreateFromTask<DxfTreeNodeViewModel>(CopyCodeAndData);
        CopyObjectTreeCommand = ReactiveCommand.CreateFromTask<DxfTreeNodeViewModel>(CopyObjectTree);
        CopyCodeCommand = ReactiveCommand.CreateFromTask<DxfTreeNodeViewModel>(CopyCode);
        CopyDataCommand = ReactiveCommand.CreateFromTask<DxfTreeNodeViewModel>(CopyData);
        AddCodeTagCommand = ReactiveCommand.Create(AddCodeTag);
        RemoveCodeTagCommand = ReactiveCommand.Create<TagModel>(RemoveCodeTag);
        AddDataTagCommand = ReactiveCommand.Create(AddDataTag);
        RemoveDataTagCommand = ReactiveCommand.Create<TagModel>(RemoveDataTag);
    }

    public ICommand ExpandAllCommand { get; }

    public ICommand CollapseAllCommand { get; }

    public ICommand FilterByLineRangeCommand { get; }

    public ICommand FilterByDataCommand { get; }

    public ICommand FilterByCodeCommand { get; }

    public ICommand ResetFiltersCommand { get; }

    public ICommand ResetCodeCommand { get; }

    public ICommand ResetDataCommand { get; }

    public ICommand ReseLineNumberStartCommand { get; }

    public ICommand ResetLineNumberEndCommand { get; }

    public ICommand ResetLineRangeCommand { get; }

    public ICommand CopyCodeAndDataCommand { get; }

    public ICommand CopyObjectTreeCommand { get; }

    public ICommand CopyCodeCommand { get; }

    public ICommand CopyDataCommand { get; }
    
    public ICommand AddCodeTagCommand { get; }
    
    public ICommand RemoveCodeTagCommand { get; }
    
    public ICommand AddDataTagCommand { get; }
    
    public ICommand RemoveDataTagCommand { get; }
    
    public int OriginalStartLine { get; set; } = 1;

    public int OriginalEndLine { get; set; } = int.MaxValue;

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

    public string FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }
    
    public ObservableCollection<TagModel> CodeTags
    {
        get => _codeTags;
        private set => this.RaiseAndSetIfChanged(ref _codeTags, value);
    }

    public ObservableCollection<TagModel> DataTags
    {
        get => _dataTags;
        private set => this.RaiseAndSetIfChanged(ref _dataTags, value);
    }

    public string NewCodeTag
    {
        get => _newCodeTag;
        set => this.RaiseAndSetIfChanged(ref _newCodeTag, value);
    }

    public string NewDataTag
    {
        get => _newDataTag;
        set => this.RaiseAndSetIfChanged(ref _newDataTag, value);
    }

    public ITreeDataGridSource<DxfTreeNodeViewModel> Source => _source;

    public DxfTreeViewModel CreateFilteredView(DxfTreeNodeViewModel selectedNode)
    {
        var filteredViewModel = new DxfTreeViewModel();
        filteredViewModel.FileName = this.FileName; // Propagate filename
        filteredViewModel.HasLoadedFile = true;
        var rawTags = new List<DxfRawTag>();

        if (selectedNode.RawTag != null)
        {
            var rootTag = new DxfRawTag
            {
                GroupCode = selectedNode.RawTag.GroupCode,
                DataElement = selectedNode.RawTag.DataElement,
                IsEnabled = selectedNode.RawTag.IsEnabled,
                OriginalGroupCodeLine = selectedNode.RawTag.OriginalGroupCodeLine,
                OriginalDataLine = selectedNode.RawTag.OriginalDataLine,
                Children = new List<DxfRawTag>()
            };

            if (selectedNode.RawTag.Children != null)
            {
                foreach (var child in selectedNode.RawTag.Children)
                {
                    CopyRawTagStructure(child, rootTag);
                }
            }

            rawTags.Add(rootTag);
        }

        // Pass the original start line to maintain line numbering
        filteredViewModel._allNodes = ConvertToTreeNodes(rawTags, selectedNode.StartLine);
        // Store original line range
        filteredViewModel.OriginalStartLine = selectedNode.StartLine;
        filteredViewModel.OriginalEndLine = selectedNode.EndLine;
        filteredViewModel.LineNumberStart = selectedNode.StartLine;
        filteredViewModel.LineNumberEnd = selectedNode.EndLine;
        filteredViewModel.ApplyFilters();

        return filteredViewModel;
    }

    private void CopyRawTagStructure(DxfRawTag source, DxfRawTag parent)
    {
        var copy = new DxfRawTag
        {
            GroupCode = source.GroupCode,
            DataElement = source.DataElement,
            IsEnabled = source.IsEnabled,
            OriginalGroupCodeLine = source.OriginalGroupCodeLine,
            OriginalDataLine = source.OriginalDataLine,
            Parent = parent,
            Children = new List<DxfRawTag>()
        };

        parent.Children?.Add(copy);

        if (source.Children != null)
        {
            foreach (var child in source.Children)
            {
                CopyRawTagStructure(child, copy);
            }
        }
    }

    public void LoadDxfData(IList<DxfRawTag> sections, string fileName)
    {
        FileName = fileName;
        _expandedNodes.Clear();

        _allNodes = ConvertToTreeNodes(sections);

        if (_allNodes.Any())
        {
            var allNodes = _allNodes.SelectMany(GetAllNodes).ToList();
            OriginalEndLine = allNodes.Max(n => n.EndLine);
            LineNumberEnd = OriginalEndLine;
            OriginalStartLine = 1;
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
        ResetCode();
        ResetData();
    }

    private void ResetCode()
    {
        CodeTags.Clear();
        NewCodeTag = "";
    }

    private void ResetData()
    {
        DataTags.Clear();
        NewDataTag = "";
    }

    private void ResetLineRange()
    {
        ResetLineNumberStart();
        ResetLineNumberEnd();
    }

    private void ResetLineNumberStart()
    {
        LineNumberStart = OriginalStartLine;
    }

    private void ResetLineNumberEnd()
    {
        LineNumberEnd = OriginalEndLine;
    }

    private async Task CopyCode(DxfTreeNodeViewModel? nodeView)
    {
        if (nodeView != null)
        {
            var clipboard = GetClipboard();
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(nodeView.CodeString);
            }
        }
    }

    private async Task CopyData(DxfTreeNodeViewModel? nodeView)
    {
        if (nodeView != null)
        {
            var clipboard = GetClipboard();
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(nodeView.Data);
            }
        }
    }
    
    private void AddCodeTag()
    {
        if (!string.IsNullOrWhiteSpace(NewCodeTag))
        {
            CodeTags.Add(new TagModel(NewCodeTag));
            NewCodeTag = "";
            ApplyFilters();
        }
    }

    private void RemoveCodeTag(TagModel tag)
    {
        if (CodeTags.Remove(tag))
        {
            ApplyFilters();
        }
    }

    private void AddDataTag()
    {
        if (!string.IsNullOrWhiteSpace(NewDataTag))
        {
            DataTags.Add(new TagModel(NewDataTag));
            NewDataTag = "";
            ApplyFilters();
        }
    }

    private void RemoveDataTag(TagModel tag)
    {
        if (DataTags.Remove(tag))
        {
            ApplyFilters();
        }
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
                var text = GetFilteredTreeText(nodeView);
                await clipboard.SetTextAsync(text);
            }
        }
    }

    private string GetFilteredTreeText(DxfTreeNodeViewModel nodeView)
    {
        var sb = new StringBuilder();
        BuildFilteredTreeText(nodeView, sb);
        return sb.ToString();
    }

    private void BuildFilteredTreeText(DxfTreeNodeViewModel node, StringBuilder sb)
    {
        bool nodeMatches = MatchesFilters(node);
        bool isTypeNode = node.Code == DxfParser.DxfCodeForType;
        bool hasMatchingDescendant = node.HasChildren && HasMatchingDescendant(node.Children.ToList());

        if (nodeMatches || hasMatchingDescendant)
        {
            if (nodeMatches)
            {
                sb.AppendLine(node.OriginalGroupCodeLine);
                sb.AppendLine(node.OriginalDataLine);
            }

            if (node.HasChildren)
            {
                if (isTypeNode && nodeMatches)
                {
                    // For type nodes that match, include all children
                    foreach (var child in node.Children)
                    {
                        sb.AppendLine(child.OriginalGroupCodeLine);
                        sb.AppendLine(child.OriginalDataLine);
                        if (child.HasChildren)
                        {
                            foreach (var grandChild in child.Children)
                            {
                                BuildFilteredTreeText(grandChild, sb);
                            }
                        }
                    }
                }
                else
                {
                    // For other nodes, recursively check filters
                    foreach (var child in node.Children)
                    {
                        BuildFilteredTreeText(child, sb);
                    }
                }
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

            ResetCode();
            ResetData();
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
            var codeValue = nodeView.Code.ToString();
            if (!CodeTags.Any(t => t.Value.Equals(codeValue, StringComparison.OrdinalIgnoreCase)))
            {
                CodeTags.Add(new TagModel(codeValue));
                ApplyFilters();
            }
        }
    }

    private void FilterByData(DxfTreeNodeViewModel? nodeView)
    {
        if (nodeView != null)
        {
            var dataValue = nodeView.Data;
            if (!DataTags.Any(t => t.Value.Equals(dataValue, StringComparison.OrdinalIgnoreCase)))
            {
                DataTags.Add(new TagModel(dataValue));
                ApplyFilters();
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
            bool isTypeNode = node.Code == DxfParser.DxfCodeForType;
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

        bool matchesCode = CodeTags.Count == 0 || 
                           CodeTags.Any(tag => nodeView.CodeString.Equals(tag.Value, StringComparison.OrdinalIgnoreCase));

        bool matchesData = DataTags.Count == 0 ||
                           DataTags.Any(tag => nodeView.Data.Contains(tag.Value, StringComparison.OrdinalIgnoreCase));

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

    private static void AddChildNodes(DxfTreeNodeViewModel parent, IList<DxfRawTag> children, ref int lineNumber)
    {
        int startLine = lineNumber;
        foreach (var child in children.Where(c => c.IsEnabled))
        {
            string type = child.GroupCode == DxfParser.DxfCodeForType
                ? child.DataElement ?? "TYPE"
                : child.GroupCode.ToString();

            var node = new DxfTreeNodeViewModel(
                lineNumber,
                lineNumber + 1,
                child.GroupCode,
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

        if (parent.Children.Any())
        {
            parent.EndLine = lineNumber - 1;
            parent.UpdateLineRange(startLine, lineNumber - 1);
        }
    }

    private static List<DxfTreeNodeViewModel> ConvertToTreeNodes(IList<DxfRawTag> sections, int startLineNumber = 1)
    {
        var nodes = new List<DxfTreeNodeViewModel>();
        var lineNumber = startLineNumber;

        foreach (var section in sections)
        {
            if (!section.IsEnabled)
            {
                continue;
            }

            int sectionStart = lineNumber;
            var sectionNode = new DxfTreeNodeViewModel(
                lineNumber,
                lineNumber + 1,
                section.GroupCode,
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

            if (sectionNode.Children.Any())
            {
                sectionNode.EndLine = lineNumber - 1;
                sectionNode.UpdateLineRange(sectionStart, lineNumber - 1);
            }

            nodes.Add(sectionNode);
        }

        return nodes;
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
