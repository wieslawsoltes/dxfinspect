using System.Collections.Generic;

namespace dxfInspect.Model;

public class DxfRawTag
{
    // Store line number and original content
    public int LineNumber { get; set; }
    public string OriginalGroupCodeLine { get; set; } = string.Empty;
    public string OriginalDataLine { get; set; } = string.Empty;

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
}
