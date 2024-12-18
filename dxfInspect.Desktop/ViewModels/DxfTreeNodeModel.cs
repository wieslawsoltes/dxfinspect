using System.Collections.ObjectModel;
using ReactiveUI;

namespace dxfInspect.Desktop.ViewModels;

public class DxfTreeNodeModel : ReactiveObject
{
    private bool _isExpanded;

    public string LineNumberRange { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Code { get; set; }
    public string Data { get; set; }
    public string Type { get; set; }
    public string NodeKey { get; set; }
    public ObservableCollection<DxfTreeNodeModel> Children { get; }
    public bool HasChildren => Children.Count > 0;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public DxfTreeNodeModel(int startLine, int endLine, string code, string data, string type, string nodeKey)
    {
        StartLine = startLine;
        EndLine = endLine;
        LineNumberRange = $"{startLine}-{endLine}";
        Code = code;
        Data = data;
        Type = type;
        NodeKey = nodeKey;
        Children = new ObservableCollection<DxfTreeNodeModel>();
    }
}
