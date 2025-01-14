using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;

namespace dxfInspect.ViewModels;

public class DxfTreeNodeViewModel : ReactiveObject
{
    private bool _isExpanded;

    public DxfTreeNodeViewModel(int startLine, int endLine, string code, string data, string type, string nodeKey)
    {
        StartLine = startLine;
        EndLine = endLine;
        Code = code;
        Data = data;
        Type = type;
        NodeKey = nodeKey;
    }

    public int StartLine { get; }
    public int EndLine { get; }
    public string Code { get; }
    public string Data { get; }
    public string Type { get; }
    public string NodeKey { get; }
    public ObservableCollection<DxfTreeNodeViewModel> Children { get; } = [];
    public bool HasChildren => Children.Count > 0;

    public string LineNumberRange
    {
        get
        {
            if (!HasChildren)
            {
                return $"{StartLine}-{EndLine}";
            }
            
            int lastLine = GetLastLineNumber(this);
            return $"{StartLine}-{lastLine}";
        }
    }

    private int GetLastLineNumber(DxfTreeNodeViewModel node)
    {
        if (!node.HasChildren)
        {
            return node.EndLine;
        }

        return node.Children
            .Select(child => GetLastLineNumber(child))
            .Max();
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }
}
