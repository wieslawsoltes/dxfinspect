using System;
using ReactiveUI;

namespace dxfInspect.ViewModels;

public class DxfTabViewModel : ReactiveObject
{
    private string _title;
    private bool _isSelected;

    public DxfTabViewModel(string title, DxfTreeViewModel content)
    {
        _title = title;
        Content = content;
    }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public DxfTreeViewModel Content { get; }
}
