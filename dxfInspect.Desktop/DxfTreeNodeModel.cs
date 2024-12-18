using System.Collections.ObjectModel;

public class DxfTreeNodeModel
{
    public string Code { get; set; }
    public string Data { get; set; }
    public string Type { get; set; }
    public bool IsExpanded { get; set; }
    public ObservableCollection<DxfTreeNodeModel> Children { get; }
    public bool HasChildren => Children.Count > 0;

    public DxfTreeNodeModel(string code, string data, string type)
    {
        Code = code;
        Data = data;
        Type = type;
        Children = new ObservableCollection<DxfTreeNodeModel>();
    }
}
