using System.Collections.ObjectModel;
using ReactiveUI;

namespace dxfInspect.ViewModels;

public class DxfTreeNodeViewModel(int startLine, int endLine, string code, string data, string type, string nodeKey)
    : ReactiveObject
{
    private bool _isExpanded;

    public string LineNumberRange { get; } = $"{startLine}-{endLine}";
    public int StartLine { get; } = startLine;
    public int EndLine { get; } = endLine;
    public string Code { get; } = code;
    public string Data { get; } = data;
    public string Type { get; } = type;
    public string NodeKey { get; } = nodeKey;
    public ObservableCollection<DxfTreeNodeViewModel> Children { get; } = [];
    public bool HasChildren => Children.Count > 0;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }
}
