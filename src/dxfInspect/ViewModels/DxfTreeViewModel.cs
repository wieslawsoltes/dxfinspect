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
using DynamicData;
using ReactiveUI;

namespace dxfInspect.ViewModels;

public class DxfTreeViewModel : ReactiveObject
{
    private readonly SourceCache<DxfTreeNodeViewModel, int> _allNodesCache;
    private ReadOnlyObservableCollection<DxfTreeNodeViewModel> _filteredCollection;
    private readonly HierarchicalTreeDataGridSource<DxfTreeNodeViewModel> _source;
    private readonly Dictionary<string, DxfTreeNodeViewModel> _nodeCache = new();
    private bool _isExpanding;
    private bool _isCollapsing;
    private readonly HashSet<string> _expandedNodes = [];
    private bool _isFiltering;
    private bool _hasLoadedFile;
    private string _fileName = "-";
    private string _newCodeTag = "";
    private string _newDataTag = "";
    private ObservableCollection<string> _uniqueCodeValues = new();
    private ObservableCollection<string> _uniqueDataValues = new();

    public DxfTreeViewModel()
    {
        Filters = new DxfTreeFiltersViewModel();
        
        _allNodesCache = new SourceCache<DxfTreeNodeViewModel, int>(x => x.StartLine);

        _allNodesCache.Connect() 
            .Filter(Filters.Filter)
            .Bind(out _filteredCollection)
            .Subscribe();
   
        _source = new HierarchicalTreeDataGridSource<DxfTreeNodeViewModel>(_filteredCollection)
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
                            CompareDescending = Sort<DxfTreeNodeViewModel>.Descending(x => x.LineRange),
                        },
                        width: new GridLength(200)),
                    x => x.Children,
                    x => x.HasChildren,
                    x => x.IsExpanded),
                new TextColumn<DxfTreeNodeViewModel, string>(
                    "Code",
                    x => x.CodeString,
                    (x, y) => x.Code = int.Parse(y),
                    options: new()
                    {
                        CompareAscending = Sort<DxfTreeNodeViewModel>.Ascending(x => x.Code),
                        CompareDescending = Sort<DxfTreeNodeViewModel>.Descending(x => x.Code)
                    },
                    width: new GridLength(80)),
                new TextColumn<DxfTreeNodeViewModel, string>(
                    "Data",
                    x => x.Data,
                    (x, y) => x.Data = y,
                    options: new()
                    {
                        CompareAscending = Sort<DxfTreeNodeViewModel>.Ascending(x => x.Data),
                        CompareDescending = Sort<DxfTreeNodeViewModel>.Descending(x => x.Data)
                    },
                    width: new GridLength(2, GridUnitType.Star)),
                new TextColumn<DxfTreeNodeViewModel, int>(
                    "Objects",
                    x => x.ObjectCount,
                    options: new()
                    {
                        CompareAscending = Sort<DxfTreeNodeViewModel>.Ascending(x => x.ObjectCount),
                        CompareDescending = Sort<DxfTreeNodeViewModel>.Descending(x => x.ObjectCount)
                    },
                    width: new GridLength(80)),
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

        ExpandAllCommand = ReactiveCommand.CreateFromTask(ExpandAllAsync);
        CollapseAllCommand = ReactiveCommand.CreateFromTask(CollapseAllAsync);
        CopyFilteredObjectTreeCommand = ReactiveCommand.CreateFromTask(CopyFilteredObjectTree);
        FilterByLineRangeCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByLineRange);
        FilterByDataCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByData);
        FilterByCodeCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(FilterByCode);
        ResetFiltersCommand = ReactiveCommand.Create(ResetFilters);
        ResetCodeCommand = ReactiveCommand.Create(ResetCode);
        ResetDataCommand = ReactiveCommand.Create(ResetData);
        ReseLineNumberStartCommand = ReactiveCommand.Create(Filters.ResetLineNumberStart);
        ResetLineNumberEndCommand = ReactiveCommand.Create(Filters.ResetLineNumberEnd);
        ResetLineRangeCommand = ReactiveCommand.Create(ResetLineRange);
        CopyCodeAndDataCommand = ReactiveCommand.CreateFromTask<DxfTreeNodeViewModel>(CopyCodeAndData);
        CopyObjectTreeCommand = ReactiveCommand.CreateFromTask<DxfTreeNodeViewModel>(CopyObjectTree);
        CopyCodeCommand = ReactiveCommand.CreateFromTask<DxfTreeNodeViewModel>(CopyCode);
        CopyDataCommand = ReactiveCommand.CreateFromTask<DxfTreeNodeViewModel>(CopyData);
        AddCodeTagCommand = ReactiveCommand.Create(AddCodeTag);
        RemoveCodeTagCommand = ReactiveCommand.Create<TagModel>(RemoveCodeTag);
        AddDataTagCommand = ReactiveCommand.Create(AddDataTag);
        RemoveDataTagCommand = ReactiveCommand.Create<TagModel>(RemoveDataTag);
        RemoveNodeCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(RemoveNode);
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
    
    public ICommand RemoveNodeCommand { get; }

    public DxfTreeFiltersViewModel Filters { get; }

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
   
    public static DxfTreeViewModel CreateFilteredView(DxfTreeNodeViewModel selectedNode, string fileName, Action<int>? progressCallback = null)
    {
        var filteredViewModel = new DxfTreeViewModel();

        filteredViewModel.FileName = fileName;
        filteredViewModel.HasLoadedFile = false;

        filteredViewModel._isFiltering = true;
        filteredViewModel.Filters.Disable();
        using var _ = filteredViewModel._allNodesCache.SuspendNotifications();
        
        var rawTags = new List<DxfRawTag>();
        var rootTag = DxfRawTagCache.Instance.GetOrCreate(selectedNode.RawTag);
        rawTags.Add(rootTag);

        var processedNodes = 0;
        var nodes = ConvertToTreeNodes(
            rawTags,
            filteredViewModel.Filters,
            selectedNode.StartLine, () =>
            {
                processedNodes++;
                progressCallback?.Invoke(processedNodes);
            });

        filteredViewModel._allNodesCache.Edit(x =>
        {
            x.AddOrUpdate(nodes);
        });
   
        filteredViewModel.HasLoadedFile = true;

        filteredViewModel._isFiltering = false;
        
        var allNodes = filteredViewModel._allNodesCache.Items.SelectMany(GetAllNodes).ToList();

        filteredViewModel.Filters.OriginalStartLine = selectedNode.StartLine;
        filteredViewModel.Filters.OriginalEndLine = selectedNode.EndLine;
        filteredViewModel.Filters.LineNumberStart = selectedNode.StartLine;
        filteredViewModel.Filters.LineNumberEnd = selectedNode.EndLine;

        filteredViewModel.Initialize(allNodes);

        filteredViewModel.Filters.Enable();

        return filteredViewModel;
    }

    public void LoadDxfData(IList<DxfRawTag> sections, string fileName, Action<int>? progressCallback = null)
    {
        _expandedNodes.Clear();
        _nodeCache.Clear();

        FileName = fileName;
        HasLoadedFile = false;

        _isFiltering = true;
        Filters.Disable();
        using var _ = _allNodesCache.SuspendNotifications();

        _allNodesCache.Edit(x =>
        {
            x.Clear();

            // Cache all raw tags without clearing existing cache
            var cachedSections = sections.Select(s => DxfRawTagCache.Instance.GetOrCreate(s)).ToList();

            var processedNodes = 0;
            var nodes = ConvertToTreeNodes(cachedSections, Filters, 1, () =>
            {
                processedNodes++;
                progressCallback?.Invoke(processedNodes);
            });

            // Add nodes to the source list
            x.AddOrUpdate(nodes);
        });
  
        HasLoadedFile = true;

        _isFiltering = false;

        var allNodes = _allNodesCache.Items.SelectMany(GetAllNodes).ToList();

        Filters.OriginalStartLine = 1;
        Filters.OriginalEndLine = allNodes.Max(n => n.EndLine);
        Filters.LineNumberEnd = Filters.OriginalEndLine;
        Filters.LineNumberStart = 1;

        Initialize(allNodes);
   
        Filters.Enable();

        // Optionally run garbage collection on the cache
        DxfRawTagCache.Instance.CollectGarbage();
    }

    public void Initialize(IReadOnlyList<DxfTreeNodeViewModel> allNodes)
    {
        foreach (var node in allNodes)
        {
            _nodeCache[node.NodeKey] = node;
        }

        var uniqueCodes = new HashSet<string>();
        var uniqueData = new HashSet<string>();

        foreach (var node in allNodes)
        {
            uniqueCodes.Add(node.CodeString);
            uniqueData.Add(node.Data);
        }

        UniqueCodeValues = new ObservableCollection<string>(uniqueCodes.OrderBy(x => x));
        UniqueDataValues = new ObservableCollection<string>(uniqueData.OrderBy(x => x));

        InitializeFiltering(_allNodesCache.Items);
        UpdateObjectCount(_allNodesCache.Items);
        UpdateTotalDataSize(_allNodesCache.Items);
    }

    private void InitializeFiltering(IEnumerable<DxfTreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            node.InitializeFiltering();

            if (node.Nodes.Any())
            {
                InitializeFiltering(node.Nodes);
            }
        }
    }

    private void UpdateObjectCount(IEnumerable<DxfTreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            node.UpdateObjectCount();

            if (node.HasChildren)
            {
                UpdateObjectCount(node.Children);
            }
        }
    }

    private void UpdateTotalDataSize(IEnumerable<DxfTreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            node.UpdateTotalDataSize();

            if (node.HasChildren)
            {
                UpdateTotalDataSize(node.Children);
            }
        }
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

            // TODO:
            /*
            await Task.Run(() =>
            {
                _expandedNodes.Clear();
                foreach (var node in _allNodesCache.Items)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    ExpandAllNodes([node]);
                }
            }, cancellationToken);
            */

            _source.ExpandAll();
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

            // TODO:
            /*
            await Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                CollapseAllNodes(_allNodesCache.Items);
            }, cancellationToken);
            */

            _source.CollapseAll();
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
        ResetCode();
        Filters.ResetCodeFilterOptions();
        ResetData();
        Filters.ResetDataFilterOptions();
    }

    private void ResetCode()
    {
        Filters.CodeTags.Clear();
        NewCodeTag = "";
    }

    private void ResetData()
    {
        Filters.DataTags.Clear();
        NewDataTag = "";
    }

    private void ResetLineRange()
    {
        Filters.ResetLineNumberStart();
        Filters.ResetLineNumberEnd();
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
            Filters.CodeTags.Add(new TagModel(NewCodeTag));
            NewCodeTag = "";
        }
    }

    private void RemoveCodeTag(TagModel tag)
    {
        Filters.CodeTags.Remove(tag);
    }

    private void AddDataTag()
    {
        if (!string.IsNullOrWhiteSpace(NewDataTag))
        {
            Filters.DataTags.Add(new TagModel(NewDataTag));
            NewDataTag = "";
        }
    }

    private void RemoveDataTag(TagModel tag)
    {
        Filters.DataTags.Remove(tag);
    }

    private void RemoveNode(DxfTreeNodeViewModel? node)
    {
        if (node == null) return;

        // Remove from cache
        RemoveNodeAndChildrenFromCache(node);

        // Remove from parent's children collection
        if (node.Parent != null)
        {
            node.Parent.RemoveChild(node);
            node.Parent.UpdateTotalDataSize();
        }
        else
        {
            _allNodesCache.Remove(node);
        }
    }

    private void RemoveNodeAndChildrenFromCache(DxfTreeNodeViewModel node)
    {
        // Remove this node from cache
        _nodeCache.Remove(node.NodeKey);
    
        // Remove all children recursively
        if (node.HasChildren)
        {
            foreach (var child in node.Children.ToList())
            {
                RemoveNodeAndChildrenFromCache(child);
            }
        }

        // If this node has a RawTag, remove it from the DxfRawTagCache
        if (node.RawTag != null)
        {
            var key = DxfRawTagCache.Instance.GenerateKey(node.RawTag);
            DxfRawTagCache.Instance.RemoveTag(key);
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
        bool nodeMatches = Filters.MatchesFilters(node);
        bool isTypeNode = node.Code == DxfParser.DxfCodeForType;
        bool hasMatchingDescendant = node.HasChildren && HasMatchingDescendant(node.Children);

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

            Filters.LineNumberStart = startLine;
            Filters.LineNumberEnd = endLine;
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
            if (!Filters.CodeTags.Any(t => t.Value.Equals(codeValue, StringComparison.OrdinalIgnoreCase)))
            {
                Filters.CodeTags.Add(new TagModel(codeValue));
            }
        }
    }

    private void FilterByData(DxfTreeNodeViewModel? nodeView)
    {
        if (nodeView != null)
        {
            var dataValue = nodeView.Data;
            if (!Filters.DataTags.Any(t => t.Value.Equals(dataValue, StringComparison.OrdinalIgnoreCase)))
            {
                Filters.DataTags.Add(new TagModel(dataValue));
            }
        }
    }

    private static IEnumerable<DxfTreeNodeViewModel> GetAllNodes(DxfTreeNodeViewModel node)
    {
        yield return node;

        if (node.Nodes.Any())
        {
            foreach (var child in node.Nodes)
            {
                foreach (var descendant in GetAllNodes(child))
                {
                    yield return descendant;
                }
            }
        }
    }

    private bool HasMatchingDescendant(IReadOnlyList<DxfTreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (Filters.MatchesFilters(node)) return true;
            if (node.HasChildren && HasMatchingDescendant(node.Children)) return true;
        }

        return false;
    }

    private static void AddChildNodes(
        DxfTreeNodeViewModel parent,
        IList<DxfRawTag> children,
        DxfTreeFiltersViewModel filters,
        ref int lineNumber,
        Action? onNodeProcessed = null)
    {
        var toAddChildren = new List<DxfTreeNodeViewModel>();
        
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
                child,
                filters)
            {
                Parent = parent  // Set the parent reference
            };
            onNodeProcessed?.Invoke();

            lineNumber += 2;

            if (child.Children != null)
            {
                AddChildNodes(node, child.Children, filters, ref lineNumber, onNodeProcessed);
            }

            toAddChildren.Add(node);
        }

        parent.AddChildRange(toAddChildren);
        
        if (toAddChildren.Any())
        {
            parent.EndLine = lineNumber - 1;
            parent.UpdateLineRange(startLine, lineNumber - 1);
        }
    }

    private static List<DxfTreeNodeViewModel> ConvertToTreeNodes(
        IList<DxfRawTag> sections,
        DxfTreeFiltersViewModel filters,
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
                section,
                filters);
            onNodeProcessed?.Invoke();

            lineNumber += 2;

            if (section.Children != null)
            {
                AddChildNodes(sectionNode, section.Children, filters, ref lineNumber, onNodeProcessed);
            }

            if (sectionNode.Nodes.Any())
            {
                sectionNode.EndLine = lineNumber - 1;
                sectionNode.UpdateLineRange(sectionStart, lineNumber - 1);
            }

            nodes.Add(sectionNode);
        }

        return nodes;
    }

    private void ExpandAllNodes(IReadOnlyList<DxfTreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.HasChildren)
            {
                node.IsExpanded = true;
                _expandedNodes.Add(node.NodeKey);
                ExpandAllNodes(node.Children);
            }
        }
    }

    private void CollapseAllNodes(IEnumerable<DxfTreeNodeViewModel> nodes)
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
