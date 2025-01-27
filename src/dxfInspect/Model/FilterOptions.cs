using ReactiveUI;

namespace dxfInspect.Model;

public class FilterOptions : ReactiveObject
{
    private bool _useExactMatch;
    private bool _ignoreCase;

    public FilterOptions(bool useExactMatch = false, bool ignoreCase = true)
    {
        _useExactMatch = useExactMatch;
        _ignoreCase = ignoreCase;
    }

    public bool UseExactMatch
    {
        get => _useExactMatch;
        set => this.RaiseAndSetIfChanged(ref _useExactMatch, value);
    }

    public bool IgnoreCase
    {
        get => _ignoreCase;
        set => this.RaiseAndSetIfChanged(ref _ignoreCase, value);
    }
}
