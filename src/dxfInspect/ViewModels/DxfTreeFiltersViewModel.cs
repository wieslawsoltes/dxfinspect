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
    private readonly Subject<Unit> _filterChanged = new();
    private bool _shouldApplyFilters;
    private int _lineNumberStart = 1;
    private int _lineNumberEnd = int.MaxValue;
    private ObservableCollection<TagModel> _codeTags = new();
    private ObservableCollection<TagModel> _dataTags = new();
    private FilterOptions _codeFilterOptions = new(useExactMatch: true, ignoreCase: true);
    private FilterOptions _dataFilterOptions = new(useExactMatch: false, ignoreCase: true);
    
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

        try
        {
            // Line range filter
            bool matchesLineRange = nodeView.StartLine >= LineNumberStart &&
                                  nodeView.EndLine <= (LineNumberEnd == 1 ? int.MaxValue : LineNumberEnd);
            if (!matchesLineRange) return false;

            // If no filters are active, show everything within line range
            if (!HasActiveFilters())
            {
                return true;
            }

            return EvaluateNodeAgainstFilters(nodeView);
        }
        catch (Exception)
        {
            // If something goes wrong during filtering, show the node
            return true;
        }
    }

    private bool HasActiveFilters()
    {
        return CodeTags.Count > 0 || DataTags.Count > 0;
    }

    private bool EvaluateNodeAgainstFilters(DxfTreeNodeViewModel node)
    {
        bool hasCodeFilters = CodeTags.Count > 0;
        bool hasDataFilters = DataTags.Count > 0;

        // Keep track of matches for each filter type
        bool? codeMatch = null;
        bool? dataMatch = null;

        // Check current node
        if (hasCodeFilters)
        {
            codeMatch = MatchesCodeFilters(node);
        }

        if (hasDataFilters)
        {
            dataMatch = MatchesDataFilters(node);
        }

        // If node matches all active filters directly
        if ((!hasCodeFilters || codeMatch == true) && (!hasDataFilters || dataMatch == true))
        {
            return true;
        }

        // Check ancestors
        var current = node.Parent;
        while (current != null)
        {
            if (hasCodeFilters && !codeMatch.GetValueOrDefault())
            {
                if (MatchesCodeFilters(current))
                {
                    codeMatch = true;
                }
            }

            if (hasDataFilters && !dataMatch.GetValueOrDefault())
            {
                if (MatchesDataFilters(current))
                {
                    dataMatch = true;
                }
            }

            // If we've found matches for all active filters, we can stop
            if ((!hasCodeFilters || codeMatch == true) && (!hasDataFilters || dataMatch == true))
            {
                return true;
            }

            current = current.Parent;
        }

        // Check descendants recursively
        if (CheckDescendantsForMatches(node, hasCodeFilters && !codeMatch.GetValueOrDefault(),
            hasDataFilters && !dataMatch.GetValueOrDefault()))
        {
            return true;
        }

        // If we have both types of filters, we need matches for both
        if (hasCodeFilters && hasDataFilters)
        {
            return codeMatch == true && dataMatch == true;
        }

        // If we only have one type of filter, we need a match for that type
        return (hasCodeFilters && codeMatch == true) || (hasDataFilters && dataMatch == true);
    }

    private bool CheckDescendantsForMatches(DxfTreeNodeViewModel node, bool needsCodeMatch, bool needsDataMatch)
    {
        if (!node.Nodes.Any() || (!needsCodeMatch && !needsDataMatch))
        {
            return false;
        }

        foreach (var child in node.Nodes)
        {
            bool foundCodeMatch = !needsCodeMatch || MatchesCodeFilters(child);
            bool foundDataMatch = !needsDataMatch || MatchesDataFilters(child);

            if ((foundCodeMatch && foundDataMatch) || 
                CheckDescendantsForMatches(child, needsCodeMatch && !foundCodeMatch, 
                    needsDataMatch && !foundDataMatch))
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchesCodeFilters(DxfTreeNodeViewModel node)
    {
        return CodeTags.Any(tag => MatchesFilter(node.CodeString, tag.Value, CodeFilterOptions));
    }

    private bool MatchesDataFilters(DxfTreeNodeViewModel node)
    {
        return DataTags.Any(tag => MatchesFilter(node.Data, tag.Value, DataFilterOptions));
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

    private void NotifyFilterChanged()
    {
        if (_shouldApplyFilters)
        {
            _filterChanged.OnNext(Unit.Default);
        }
    }
}
