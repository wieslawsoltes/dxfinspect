using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
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
    private readonly Dictionary<string, DxfTreeNodeViewModel> _nodeCache = new();
    private readonly HierarchicalTreeDataGridSource<DxfTreeNodeViewModel> _source;
    private bool _isExpanding;
    private bool _isCollapsing;
    private readonly HashSet<string> _expandedNodes = [];
    private int _lineNumberStart = 1;
    private int _lineNumberEnd = int.MaxValue;
    private List<DxfTreeNodeViewModel> _allNodes = [];
    private bool _hasLoadedFile;
    private string _fileName = "-";
    private ObservableCollection<TagModel> _codeTags;
    private ObservableCollection<TagModel> _dataTags;
    private string _newCodeTag = "";
    private string _newDataTag = "";
    private FilterOptions _codeFilterOptions;
    private FilterOptions _dataFilterOptions;
    private ObservableCollection<string> _uniqueCodeValues = new();
    private ObservableCollection<string> _uniqueDataValues = new();
    private bool _shouldApplyFilters;

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
                    "Size",
                    x => x.FormattedDataSize,
                    options: new()
                    {
                        CompareAscending = Sort<DxfTreeNodeViewModel>.Ascending(x => x.TotalDataSize),
                        CompareDescending = Sort<DxfTreeNodeViewModel>.Descending(x => x.TotalDataSize)
                    },
                    width: new GridLength(100)),
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
        _codeFilterOptions = new FilterOptions(useExactMatch: true, ignoreCase: true);
        _dataFilterOptions = new FilterOptions(useExactMatch: false, ignoreCase: true);

        this.WhenAnyValue(x => x.CodeFilterOptions.UseExactMatch,
                x => x.CodeFilterOptions.IgnoreCase)
            .Subscribe(_ => ApplyFilters());

        this.WhenAnyValue(x => x.DataFilterOptions.UseExactMatch,
                x => x.DataFilterOptions.IgnoreCase)
            .Subscribe(_ => ApplyFilters());

        // Set up reactive properties to trigger filter application
        this.WhenAnyValue(
                x => x.LineNumberStart,
                x => x.LineNumberEnd,
                x => x._shouldApplyFilters)
            .Where(_ => _shouldApplyFilters)
            .Subscribe(_ => ApplyFilters());

        // Monitor code tags collection changes
        this.WhenAnyValue(x => x.CodeTags.Count)
            .Where(_ => _shouldApplyFilters)
            .Subscribe(_ => ApplyFilters());

        // Monitor data tags collection changes
        this.WhenAnyValue(x => x.DataTags.Count)
            .Where(_ => _shouldApplyFilters)
            .Subscribe(_ => ApplyFilters());

        // Set up filter options monitoring
        this.WhenAnyValue(
                x => x.CodeFilterOptions.UseExactMatch,
                x => x.CodeFilterOptions.IgnoreCase,
                x => x.DataFilterOptions.UseExactMatch,
                x => x.DataFilterOptions.IgnoreCase)
            .Where(_ => _shouldApplyFilters)
            .Subscribe(_ => ApplyFilters());

        ExpandAllCommand = ReactiveCommand.CreateFromTask(ExpandAllAsync);
        CollapseAllCommand = ReactiveCommand.CreateFromTask(CollapseAllAsync);
        CopyFilteredObjectTreeCommand = ReactiveCommand.CreateFromTask(CopyFilteredObjectTree);
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
    
    public ICommand CopyFilteredObjectTreeCommand { get; }
    
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

    public int LineNumberStart
    {
        get => _lineNumberStart;
        set => this.RaiseAndSetIfChanged(ref _lineNumberStart, value);
    }

    public int LineNumberEnd
    {
        get => _lineNumberEnd;
        set => this.RaiseAndSetIfChanged(ref _lineNumberEnd, value);
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

    public FilterOptions CodeFilterOptions
    {
        get => _codeFilterOptions;
        set => this.RaiseAndSetIfChanged(ref _codeFilterOptions, value);
    }

    public FilterOptions DataFilterOptions
    {
        get => _dataFilterOptions;
        set => this.RaiseAndSetIfChanged(ref _dataFilterOptions, value);
    }

    public ObservableCollection<string> UniqueCodeValues
    {
        get => _uniqueCodeValues;
        private set => this.RaiseAndSetIfChanged(ref _uniqueCodeValues, value);
    }

    public ObservableCollection<string> UniqueDataValues
    {
        get => _uniqueDataValues;
        private set => this.RaiseAndSetIfChanged(ref _uniqueDataValues, value);
    }

    public ITreeDataGridSource<DxfTreeNodeViewModel> Source => _source;
    
    public DxfTreeViewModel CreateFilteredView(DxfTreeNodeViewModel selectedNode)
    {
        var filteredViewModel = new DxfTreeViewModel();
        filteredViewModel.FileName = this.FileName;
        filteredViewModel.HasLoadedFile = true;
        var rawTags = new List<DxfRawTag>();

        if (selectedNode.RawTag != null)
        {
            // Use the cache to get the raw tag - no need to copy
            var rootTag = DxfRawTagCache.Instance.GetOrCreate(selectedNode.RawTag);
            rawTags.Add(rootTag);

            // Rest of the initialization code...
            filteredViewModel.OriginalStartLine = selectedNode.StartLine;
            filteredViewModel.OriginalEndLine = selectedNode.EndLine;
            filteredViewModel.LineNumberStart = selectedNode.StartLine;
            filteredViewModel.LineNumberEnd = selectedNode.EndLine;

            filteredViewModel._allNodes = ConvertToTreeNodes(rawTags, selectedNode.StartLine);

            foreach (var node in filteredViewModel._allNodes.SelectMany(n => GetAllNodes(n)))
            {
                filteredViewModel._nodeCache[node.NodeKey] = node;
            }

            // Build unique values for filtering
            var uniqueCodes = new HashSet<string>();
            var uniqueData = new HashSet<string>();

            foreach (var node in filteredViewModel._allNodes.SelectMany(n => GetAllNodes(n)))
            {
                uniqueCodes.Add(node.CodeString);
                uniqueData.Add(node.Data);
            }

            filteredViewModel.UniqueCodeValues = new ObservableCollection<string>(uniqueCodes.OrderBy(x => x));
            filteredViewModel.UniqueDataValues = new ObservableCollection<string>(uniqueData.OrderBy(x => x));

            // Copy existing filters if any
            if (CodeTags.Any() || DataTags.Any())
            {
                foreach (var tag in CodeTags)
                {
                    filteredViewModel.CodeTags.Add(new TagModel(tag.Value));
                }
                foreach (var tag in DataTags)
                {
                    filteredViewModel.DataTags.Add(new TagModel(tag.Value));
                }

                filteredViewModel.CodeFilterOptions.UseExactMatch = CodeFilterOptions.UseExactMatch;
                filteredViewModel.CodeFilterOptions.IgnoreCase = CodeFilterOptions.IgnoreCase;
                filteredViewModel.DataFilterOptions.UseExactMatch = DataFilterOptions.UseExactMatch;
                filteredViewModel.DataFilterOptions.IgnoreCase = DataFilterOptions.IgnoreCase;
            }

            filteredViewModel._shouldApplyFilters = true;
            filteredViewModel.ApplyFilters();
        }

        return filteredViewModel;
    }

    public void LoadDxfData(IList<DxfRawTag> sections, string fileName, Action<int>? progressCallback = null)
    {
        _shouldApplyFilters = false;
        try
        {
            FileName = fileName;
            _expandedNodes.Clear();
            _nodeCache.Clear();

            // Cache all raw tags without clearing existing cache
            var cachedSections = new List<DxfRawTag>();
            foreach (var section in sections)
            {
                var cachedSection = DxfRawTagCache.Instance.GetOrCreate(section);
                cachedSections.Add(cachedSection);
            }

            var processedNodes = 0;
            _allNodes = ConvertToTreeNodes(cachedSections, 1, () =>
            {
                processedNodes++;
                progressCallback?.Invoke(processedNodes);
            });

            // Rest of the initialization code...
            if (_allNodes.Any())
            {
                var allNodes = _allNodes.SelectMany(GetAllNodes).ToList();
                OriginalEndLine = allNodes.Max(n => n.EndLine);
                LineNumberEnd = OriginalEndLine;
                OriginalStartLine = 1;
                LineNumberStart = 1;

                foreach (var node in allNodes)
                {
                    _nodeCache[node.NodeKey] = node;
                }

                var codes = new HashSet<string>();
                var data = new HashSet<string>();

                foreach (var node in allNodes)
                {
                    codes.Add(node.CodeString);
                    data.Add(node.Data);
                }

                UniqueCodeValues = new ObservableCollection<string>(codes.OrderBy(x => x));
                UniqueDataValues = new ObservableCollection<string>(data.OrderBy(x => x));
            }

            HasLoadedFile = true;
            _shouldApplyFilters = true;
            ApplyFilters();
        }
        finally
        {
            _shouldApplyFilters = true;
        }

        // Optionally run garbage collection on the cache
        DxfRawTagCache.Instance.CollectGarbage();
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
    
    private async Task CopyFilteredObjectTree()
    {
        var clipboard = GetClipboard();
        if (clipboard == null) return;

        var sb = new StringBuilder();
        foreach (var node in _source.Items)
        {
            BuildTreeText(node, sb);
        }

        await clipboard.SetTextAsync(sb.ToString());
    }

    private void ResetFilters()
    {
        _shouldApplyFilters = false;
        try
        {
            ResetCode();
            ResetCodeFilterOptions();
            ResetData();
            ResetDataFilterOptions();
        }
        finally
        {
            _shouldApplyFilters = true;
            ApplyFilters();
        }
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
        _shouldApplyFilters = false;
        try
        {
            ResetLineNumberStart();
            ResetLineNumberEnd();
        }
        finally
        {
            _shouldApplyFilters = true;
            ApplyFilters();
        }
    }

    private void ResetLineNumberStart()
    {
        LineNumberStart = OriginalStartLine;
    }

    private void ResetLineNumberEnd()
    {
        LineNumberEnd = OriginalEndLine;
    }

    public void ResetCodeFilterOptions()
    {
        CodeFilterOptions.UseExactMatch = true;
        CodeFilterOptions.IgnoreCase = true;
    }

    public void ResetDataFilterOptions()
    {
        DataFilterOptions.UseExactMatch = false;
        DataFilterOptions.IgnoreCase = true;
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
            _shouldApplyFilters = false;
            try
            {
                CodeTags.Add(new TagModel(NewCodeTag));
                NewCodeTag = "";
            }
            finally
            {
                _shouldApplyFilters = true;
                ApplyFilters();
            }
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
            _shouldApplyFilters = false;
            try
            {
                DataTags.Add(new TagModel(NewDataTag));
                NewDataTag = "";
            }
            finally
            {
                _shouldApplyFilters = true;
                ApplyFilters();
            }
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

    private void BuildTreeText(DxfTreeNodeViewModel node, StringBuilder sb)
    {
        sb.AppendLine(node.OriginalGroupCodeLine);
        sb.AppendLine(node.OriginalDataLine);

        if (node.HasChildren)
        {
            foreach (var child in node.Children)
            {
                BuildTreeText(child, sb);
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

            // TODO:
            // ResetCode();
            // ResetData();
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
        if (!_shouldApplyFilters || !HasLoadedFile)
        {
            return;
        }

        // Create new filtered nodes
        var filteredNodes = FilterNodes(_allNodes);

        // Important: Update the source with new nodes
        _source.Items = filteredNodes;
    }

    private List<DxfTreeNodeViewModel> FilterNodes(List<DxfTreeNodeViewModel> nodes)
    {
        var result = new List<DxfTreeNodeViewModel>();
        var visibleNodes = new HashSet<string>();

        // First pass: Mark all nodes that match the filters and their ancestors
        foreach (var node in _allNodes.SelectMany(GetAllNodes))
        {
            if (MatchesFilters(node))
            {
                // Mark this node and all its ancestors as visible
                MarkNodeAndAncestorsVisible(node, visibleNodes);
            }
        }

        // Second pass: Create filtered tree with visible nodes
        foreach (var node in nodes)
        {
            if (visibleNodes.Contains(node.NodeKey))
            {
                var filteredNode = CreateFilteredNode(node, visibleNodes);
                result.Add(filteredNode);
            }
        }

        return result;
    }

    private void MarkNodeAndAncestorsVisible(DxfTreeNodeViewModel node, HashSet<string> visibleNodes)
    {
        var current = node;
        while (current != null)
        {
            visibleNodes.Add(current.NodeKey);

            // Move to parent if it exists
            current = GetParentNode(current);
        }
    }

    private DxfTreeNodeViewModel? GetParentNode(DxfTreeNodeViewModel node)
    {
        if (node.RawTag?.Parent == null) return null;

        var parentKey = GetNodeKey(node.RawTag.Parent);
        return _nodeCache.TryGetValue(parentKey, out var parentNode) ? parentNode : null;
    }

    private string GetNodeKey(DxfRawTag tag)
    {
        string type = tag.GroupCode == DxfParser.DxfCodeForType
            ? tag.DataElement ?? "TYPE"
            : tag.GroupCode.ToString();

        return $"{tag.LineNumber}:{type}:{tag.DataElement}";
    }

    private DxfTreeNodeViewModel CreateFilteredNode(DxfTreeNodeViewModel originalNode, HashSet<string> visibleNodes)
    {
        // Create a new node instance
        var newNode = new DxfTreeNodeViewModel(
            originalNode.StartLine,
            originalNode.EndLine,
            originalNode.Code,
            originalNode.Data,
            originalNode.Type,
            originalNode.NodeKey,
            originalNode.OriginalGroupCodeLine,
            originalNode.OriginalDataLine,
            originalNode.RawTag) 
        { 
            IsExpanded = _expandedNodes.Contains(originalNode.NodeKey)
        };

        // Add visible children
        if (originalNode.HasChildren)
        {
            foreach (var child in originalNode.Children)
            {
                if (visibleNodes.Contains(child.NodeKey))
                {
                    var newChild = CreateFilteredNode(child, visibleNodes);
                    newChild.Parent = newNode;  // Set parent reference
                    newNode.Children.Add(newChild);
                }
            }
        }

        newNode.UpdateTotalDataSize();
        return newNode;
    }
    
    private bool MatchesFilters(DxfTreeNodeViewModel nodeView)
    {
        // Line range filter - this is always required
        bool matchesLineRange = nodeView.StartLine >= LineNumberStart &&
                                nodeView.EndLine <= (LineNumberEnd == 1 ? int.MaxValue : LineNumberEnd);
        if (!matchesLineRange) return false;

        // If no filters are active, show everything within line range
        if (CodeTags.Count == 0 && DataTags.Count == 0)
        {
            return true;
        }

        // If we have a Data filter
        if (DataTags.Count > 0)
        {
            bool matchesData = DataTags.Any(tag => MatchesFilter(nodeView.Data, tag.Value, DataFilterOptions));

            // Direct match with Data filter
            if (matchesData)
            {
                // If we also have Code filters and this isn't a group element,
                // it must also match a Code filter
                if (CodeTags.Count > 0 && nodeView.Code != DxfParser.DxfCodeForType)
                {
                    return CodeTags.Any(tag => MatchesFilter(nodeView.CodeString, tag.Value, CodeFilterOptions));
                }
                return true;
            }

            // For children of matching Code 0 elements
            if (nodeView.Parent?.Code == DxfParser.DxfCodeForType && 
                DataTags.Any(tag => MatchesFilter(nodeView.Parent.Data, tag.Value, DataFilterOptions)))
            {
                // If we have Code filters, only show matching children
                if (CodeTags.Count > 0)
                {
                    return CodeTags.Any(tag => MatchesFilter(nodeView.CodeString, tag.Value, CodeFilterOptions));
                }
                // If no Code filters, show all children of matching Data groups
                return true;
            }

            // Hide everything else when we have a Data filter
            return false;
        }

        // If we only have Code filters, show matching elements
        return CodeTags.Any(tag => MatchesFilter(nodeView.CodeString, tag.Value, CodeFilterOptions));
    }

    private static bool MatchesFilter(string value, string filter, FilterOptions options)
    {
        if (string.IsNullOrEmpty(filter))
            return true;

        var comparison = options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (options.UseExactMatch)
        {
            return value.Equals(filter, comparison);
        }

        return value.Contains(filter, comparison);
    }

    private IEnumerable<DxfTreeNodeViewModel> GetAllNodes(DxfTreeNodeViewModel node)
    {
        yield return node;

        if (node.HasChildren)
        {
            foreach (var child in node.Children)
            {
                foreach (var descendant in GetAllNodes(child))
                {
                    yield return descendant;
                }
            }
        }
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

    private static void AddChildNodes(
        DxfTreeNodeViewModel parent,
        IList<DxfRawTag> children,
        ref int lineNumber,
        Action? onNodeProcessed = null)
    {
        int startLine = lineNumber;
        foreach (var child in children)
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
                child)
            {
                Parent = parent  // Set the parent reference
            };
            onNodeProcessed?.Invoke();

            lineNumber += 2;

            if (child.Children != null)
            {
                AddChildNodes(node, child.Children, ref lineNumber, onNodeProcessed);
            }

            node.UpdateTotalDataSize();
            parent.Children.Add(node);
        }

        if (parent.Children.Any())
        {
            parent.EndLine = lineNumber - 1;
            parent.UpdateLineRange(startLine, lineNumber - 1);
        }
    }

    private static List<DxfTreeNodeViewModel> ConvertToTreeNodes(
        IList<DxfRawTag> sections,
        int startLineNumber = 1,
        Action? onNodeProcessed = null)
    {
        var nodes = new List<DxfTreeNodeViewModel>();
        var lineNumber = startLineNumber;

        foreach (var section in sections)
        {
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
            onNodeProcessed?.Invoke();

            lineNumber += 2;

            if (section.Children != null)
            {
                AddChildNodes(sectionNode, section.Children, ref lineNumber, onNodeProcessed);
            }

            if (sectionNode.Children.Any())
            {
                sectionNode.EndLine = lineNumber - 1;
                sectionNode.UpdateLineRange(sectionStart, lineNumber - 1);
            }

            sectionNode.UpdateTotalDataSize();
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
}
