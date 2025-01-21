using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using dxfInspect.Model;

namespace dxfInspect.Services;

/// <summary>
/// Memory-optimized parser for DXF (Drawing Exchange Format) files.
/// Uses streaming to process large files efficiently.
/// </summary>
public static class DxfParser
{
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
    /// Parses a DXF file using streaming to minimize memory usage
    /// </summary>
    public static async Task<IList<DxfRawTag>> ParseStreamAsync(Stream stream)
    {
        var sections = new List<DxfRawTag>();
        var sectionStack = new Stack<DxfRawTag>();
        var currentTag = default(DxfRawTag);

        using var reader = new StreamReader(stream);
        string? groupCodeLine;
        int lineNumber = 0;

        while ((groupCodeLine = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;
            var dataLine = await reader.ReadLineAsync();
            if (dataLine == null) break;
            lineNumber++;

            var tag = new DxfRawTag
            {
                GroupCode = int.Parse(groupCodeLine.Trim()),
                DataElement = dataLine.Trim(),
                LineNumber = lineNumber - 1,
                OriginalGroupCodeLine = groupCodeLine,
                OriginalDataLine = dataLine,
                IsEnabled = true
            };

            var isEntityWithType = tag.GroupCode == DxfCodeForType;
            var isSectionStart = isEntityWithType && tag.DataElement == DxfCodeNameSection;
            var isSectionEnd = isEntityWithType && tag.DataElement == DxfCodeNameEndsec;

            if (isSectionStart)
            {
                tag.Children = new List<DxfRawTag>();
                sections.Add(tag);
                sectionStack.Push(tag);
                currentTag = null;
            }
            else if (isSectionEnd && sectionStack.Count > 0)
            {
                tag.Parent = sectionStack.Peek();
                tag.Parent.Children?.Add(tag);
                sectionStack.Pop();
                currentTag = null;
            }
            else if (sectionStack.Count > 0)
            {
                var currentSection = sectionStack.Peek();

                if (isEntityWithType)
                {
                    tag.Parent = currentSection;
                    tag.Children = new List<DxfRawTag>();
                    currentSection.Children?.Add(tag);
                    currentTag = tag;
                }
                else if (currentTag != null)
                {
                    tag.Parent = currentTag;
                    currentTag.Children?.Add(tag);
                }
                else
                {
                    tag.Parent = currentSection;
                    currentSection.Children?.Add(tag);
                }
            }
            else
            {
                sections.Add(tag);
            }
        }

        return sections;
    }

    /// <summary>
    /// Parses a DXF file from a string (maintained for compatibility)
    /// </summary>
    public static async Task<IList<DxfRawTag>> ParseAsync(string text)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(text);
        await writer.FlushAsync();
        stream.Position = 0;
        return await ParseStreamAsync(stream);
    }
}
