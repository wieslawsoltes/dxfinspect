using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using dxfInspect.Model;
using ReactiveUI;

namespace dxfInspect.ViewModels;

public class DxfTreeFiltersViewModel : ReactiveObject
{
    private bool _shouldApplyFilters;
    private int _lineNumberStart = 1;
    private int _lineNumberEnd = int.MaxValue;
    private ObservableCollection<TagModel> _codeTags = new();
    private ObservableCollection<TagModel> _dataTags = new();
    private FilterOptions _codeFilterOptions = new(useExactMatch: true, ignoreCase: true);
    private FilterOptions _dataFilterOptions = new(useExactMatch: false, ignoreCase: true);

    private readonly Subject<Unit> _filterChanged = new();
    
    public DxfTreeFiltersViewModel()
    {
        this.WhenAnyValue(x => x.LineNumberStart)
            .Subscribe(_ => NotifyFilterChanged());
        
        this.WhenAnyValue(x => x.LineNumberEnd)
            .Subscribe(_ => NotifyFilterChanged());
        
        this.WhenAnyValue(x => x.CodeTags.Count)
            .Subscribe(_ => NotifyFilterChanged());
        
        this.WhenAnyValue(x => x.DataTags.Count)
            .Subscribe(_ => NotifyFilterChanged());
        
        this.WhenAnyValue(x => x.CodeFilterOptions.UseExactMatch)
            .Subscribe(_ => NotifyFilterChanged());
        
        this.WhenAnyValue(x => x.CodeFilterOptions.IgnoreCase)
            .Subscribe(_ => NotifyFilterChanged());
        
        this.WhenAnyValue(x => x.DataFilterOptions.UseExactMatch)
            .Subscribe(_ => NotifyFilterChanged());
        
        this.WhenAnyValue(x => x.DataFilterOptions.IgnoreCase)
            .Subscribe(_ => NotifyFilterChanged());

        Filter = _filterChanged
            .Select(_ => new Func<DxfTreeNodeViewModel, bool>(MatchesFilters))
            .StartWith(_ => true);
    }
    
    private void NotifyFilterChanged()
    {
        if (_shouldApplyFilters)
        {
            _filterChanged.OnNext(Unit.Default);
        }
    }

    public IObservable<Func<DxfTreeNodeViewModel,bool>> Filter { get; }

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

    public ObservableCollection<TagModel> CodeTags
    {
        get => _codeTags;
        set => this.RaiseAndSetIfChanged(ref _codeTags, value);
    }

    public ObservableCollection<TagModel> DataTags
    {
        get => _dataTags;
        set => this.RaiseAndSetIfChanged(ref _dataTags, value);
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

    public void Enable() => _shouldApplyFilters = true;
    
    public void Disable() => _shouldApplyFilters = false;

    public void ResetLineNumberStart()
    {
        LineNumberStart = OriginalStartLine;
    }

    public void ResetLineNumberEnd()
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

    public bool MatchesFilters(DxfTreeNodeViewModel nodeView)
    {
        if (!_shouldApplyFilters)
        {
            return true;
        }

        // Line range filter
        bool matchesLineRange = nodeView.StartLine >= LineNumberStart &&
                                nodeView.EndLine <= (LineNumberEnd == 1 ? int.MaxValue : LineNumberEnd);
        if (!matchesLineRange) return false;

        // If no filters are active, show everything within line range
        if (CodeTags.Count == 0 && DataTags.Count == 0)
        {
            return true;
        }

        // Check if this node or any of its descendants match the filters
        if (NodeMatchesFilters(nodeView) || HasMatchingDescendant(nodeView))
        {
            return true;
        }

        // Check if any ancestor matches (especially for type 0 entities)
        return HasMatchingAncestor(nodeView);
    }

    private bool NodeMatchesFilters(DxfTreeNodeViewModel node)
    {
        bool matchesCode = CodeTags.Count == 0 || 
                           CodeTags.Any(tag => MatchesFilter(node.CodeString, tag.Value, CodeFilterOptions));
    
        bool matchesData = DataTags.Count == 0 ||
                           DataTags.Any(tag => MatchesFilter(node.Data, tag.Value, DataFilterOptions));

        return matchesCode && matchesData;
    }

    private bool HasMatchingDescendant(DxfTreeNodeViewModel node)
    {
        if (!node.HasChildren) return false;

        foreach (var child in node.Children)
        {
            if (NodeMatchesFilters(child) || HasMatchingDescendant(child))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasMatchingAncestor(DxfTreeNodeViewModel node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (NodeMatchesFilters(current))
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    private bool MatchesFilter(string value, string filter, FilterOptions options)
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
}
