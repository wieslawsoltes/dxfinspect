namespace dxfInspect.Desktop;

public class DxfRow
{
    public required int LineNumber { get; set; }
    public required string Code { get; set; }
    public required string Data { get; set; }
    public required string RowType { get; set; }
    public required string SectionName { get; set; }
}
