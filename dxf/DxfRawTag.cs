using System.Text;

namespace Dxf;

public class DxfRawTag
{
    public string OriginalGroupCodeLine { get; set; } = string.Empty;

    public string OriginalDataLine { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this tag is enabled and should be included in processing
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The group code number for this tag
    /// </summary>
    public int GroupCode { get; set; }

    /// <summary>
    /// The data element/value for this tag
    /// </summary>
    public string? DataElement { get; set; }

    /// <summary>
    /// Parent tag in the hierarchy
    /// </summary>
    public DxfRawTag? Parent { get; set; }

    /// <summary>
    /// Child tags in the hierarchy
    /// </summary>
    public IList<DxfRawTag>? Children { get; set; }

    public DxfRawTag()
    {
        IsEnabled = true;
    }

    public string GetOriginalTreeText()
    {
        var sb = new StringBuilder();
        BuildOriginalTreeText(this, sb);
        return sb.ToString();
    }

    private static void BuildOriginalTreeText(DxfRawTag tag, StringBuilder sb)
    {
        if (tag.IsEnabled)
        {
            sb.AppendLine(tag.OriginalGroupCodeLine);
            sb.AppendLine(tag.OriginalDataLine);

            if (tag.Children != null)
            {
                foreach (var child in tag.Children.Where(c => c.IsEnabled))
                {
                    BuildOriginalTreeText(child, sb);
                }
            }
        }
    }
}
