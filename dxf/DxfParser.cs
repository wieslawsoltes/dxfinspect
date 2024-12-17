namespace Dxf;

/// <summary>
/// 
/// </summary>
public static class DxfParser
{
    /// <summary>
    /// 
    /// </summary>
    public const int DxfCodeForType = 0;
    
    /// <summary>
    /// 
    /// </summary>
    public const int DxfCodeForName = 2;
    
    /// <summary>
    /// 
    /// </summary>
    public const string DxfCodeNameSection = "SECTION";
    
    /// <summary>
    /// 
    /// </summary>
    public const string DxfCodeNameEndsec = "ENDSEC";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static IList<DxfRawTag> Parse(string text)
    {
        var lines = text.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();

        if (lines.Length % 2 != 0)
        {
            throw new Exception("Invalid number of lines.");
        }

        var sections = new List<DxfRawTag>();

        var section = default(DxfRawTag);
        var other = default(DxfRawTag);

        for (var i = 0; i < lines.Length; i += 2)
        {
            var tag = new DxfRawTag();
            tag.GroupCode = int.Parse(lines[i]);
            tag.DataElement = lines[i + 1];

            var isEntityWithType = tag.GroupCode == DxfCodeForType;
            var isSectionStart = (isEntityWithType) && tag.DataElement == DxfCodeNameSection;
            var isSectionEnd = (isEntityWithType) && tag.DataElement == DxfCodeNameEndsec;

            if (isSectionStart)
            {
                section = tag;
                section.Children = new List<DxfRawTag>();
                sections.Add(section);
                other = default(DxfRawTag);
            }
            else if (isSectionEnd)
            {
                tag.Parent = section;
                section.Children.Add(tag);
                section = default(DxfRawTag);
                other = default(DxfRawTag);
            }
            else
            {
                if (section != null)
                {
                    if (isEntityWithType && other == null)
                    {
                        other = tag;
                        other.Parent = section;
                        other.Children = new List<DxfRawTag>();
                        section.Children.Add(other);
                    }
                    else if (isEntityWithType && other != null)
                    {
                        other = tag;
                        other.Parent = section;
                        other.Children = new List<DxfRawTag>();
                        section.Children.Add(other);
                    }
                    else if (!isEntityWithType && other != null)
                    {
                        tag.Parent = other;
                        other.Children.Add(tag);
                    }
                    else
                    {
                        tag.Parent = section;
                        section.Children.Add(tag);
                    }
                }
                else
                {
                    tag.Parent = default(DxfRawTag);
                    sections.Add(tag);
                }
            }
        }

        return sections;
    }
}
