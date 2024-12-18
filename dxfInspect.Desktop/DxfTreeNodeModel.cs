using System.Collections.ObjectModel;

namespace dxfInspect.Desktop;

public class DxfTreeNodeModel
{
    public string LineNumberRange { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Code { get; set; }
    public string Data { get; set; }
    public string Type { get; set; }
    public bool IsExpanded { get; set; }
    public ObservableCollection<DxfTreeNodeModel> Children { get; }
    public bool HasChildren => Children.Count > 0;

    public DxfTreeNodeModel(int startLine, int endLine, string code, string data, string type)
    {
        StartLine = startLine;
        EndLine = endLine;
        LineNumberRange = $"{startLine}-{endLine}";
        Code = code;
        Data = data;
        Type = type;
        Children = new ObservableCollection<DxfTreeNodeModel>();
    }
}
