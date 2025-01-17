using System.Collections.Generic;
using System.Text.RegularExpressions;
using dxfInspect.Model;

namespace dxfInspect.Services;

/// <summary>
/// Parser for DXF (Drawing Exchange Format) files.
/// Processes DXF content into a hierarchical structure of tags.
/// </summary>
public static class DxfParser
{
    private static readonly Regex s_lineSplitter = new(@"\r\n|\r|\n", RegexOptions.Compiled);

    /// <summary>
    /// Group code indicating entity type
    /// </summary>
    public const int DxfCodeForType = 0;

    /// <summary>
    /// Group code indicating entity name
    /// </summary>
    public const int DxfCodeForName = 2;

    /// <summary>
    /// Section start marker
    /// </summary>
    public const string DxfCodeNameSection = "SECTION";

    /// <summary>
    /// Section end marker
    /// </summary>
    public const string DxfCodeNameEndsec = "ENDSEC";

    /// <summary>
    /// Parses DXF content into a hierarchical structure of tags
    /// </summary>
    /// <param name="text">Raw DXF file content</param>
    /// <returns>List of parsed DXF tags representing the file structure</returns>
    public static IList<DxfRawTag> Parse(string text)
    {
        var lines = string.IsNullOrEmpty(text) ? [] : s_lineSplitter.Split(text);
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
                IsEnabled = true
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
