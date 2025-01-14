using System.Text.RegularExpressions;

namespace Dxf;

/// <summary>
/// 
/// </summary>
public static class DxfParser
{
    private static readonly Regex s_lineSplitter = new(@"\r\n|\r|\n", RegexOptions.Compiled);

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
        var lines = string.IsNullOrEmpty(text) ? [] : s_lineSplitter.Split(text);
        Console.WriteLine($"Parsing DXF file. Total lines: {lines.Length}");

        var sections = new List<DxfRawTag>();
        var section = default(DxfRawTag);
        var other = default(DxfRawTag);

        for (var i = 0; i < lines.Length; i += 2)
        {
            if (i + 1 >= lines.Length)
            {
                break;
            }

            var groupCodeLine = lines[i];
            var dataLine = lines[i + 1];
            var groupCode = groupCodeLine.Trim();
            var dataElement = dataLine.Trim();

            var tag = new DxfRawTag
            {
                GroupCode = int.Parse(groupCode),
                DataElement = dataElement,
                OriginalGroupCodeLine = groupCodeLine,
                OriginalDataLine = dataLine,
                IsEnabled = true // Set to true for every new tag
            };

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
                if (section?.Children != null)
                {
                    section.Children.Add(tag);
                }

                section = default(DxfRawTag);
                other = default(DxfRawTag);
            }
            else if (section != null)
            {
                if (isEntityWithType && other == null)
                {
                    other = tag;
                    other.Parent = section;
                    other.Children = new List<DxfRawTag>();
                    section.Children?.Add(other);
                }
                else if (isEntityWithType && other != null)
                {
                    other = tag;
                    other.Parent = section;
                    other.Children = new List<DxfRawTag>();
                    section.Children?.Add(other);
                }
                else if (!isEntityWithType && other != null)
                {
                    tag.Parent = other;
                    other.Children?.Add(tag);
                }
                else
                {
                    tag.Parent = section;
                    section.Children?.Add(tag);
                }
            }
            else
            {
                tag.Parent = default(DxfRawTag);
                sections.Add(tag);
            }
        }

        // Enable all nodes in the hierarchy
        foreach (var dxfSection in sections)
        {
            EnableHierarchy(dxfSection);
        }

        return sections;
    }

    private static void EnableHierarchy(DxfRawTag tag)
    {
        tag.IsEnabled = true;
        if (tag.Children != null)
        {
            foreach (var child in tag.Children)
            {
                EnableHierarchy(child);
            }
        }
    }
}
