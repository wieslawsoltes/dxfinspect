using System;
using ReactiveUI;

namespace dxfInspect.Model;

public class TagModel : ReactiveObject
{
    private string _value;

    public TagModel(string value)
    {
        _value = value;
    }

    public string Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }
}
