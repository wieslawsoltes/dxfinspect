using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using dxfInspect.Model;

namespace dxfInspect.Services;

/// <summary>
/// Memory-optimized parser for DXF (Drawing Exchange Format) files.
/// Uses streaming and minimal memory allocation to process large files efficiently.
/// </summary>
public static class DxfParser
{
    public const int DxfCodeForType = 0;
    public const int DxfCodeForName = 2;
    public const string DxfCodeNameSection = "SECTION";
    public const string DxfCodeNameEndsec = "ENDSEC";

    private const int BufferSize = 4096; // Use a reasonable buffer size for reading

    /// <summary>
    /// Parses a DXF file using streaming to minimize memory usage with optimized buffering
    /// </summary>
    public static async Task<IList<DxfRawTag>> ParseStreamAsync(Stream stream)
    {
        var sections = new List<DxfRawTag>();
        var sectionStack = new Stack<DxfRawTag>();
        var currentTag = default(DxfRawTag);

        using var reader = new StreamReader(stream, bufferSize: BufferSize);
        int lineNumber = 0;

        string? groupCodeLine;
        while ((groupCodeLine = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            lineNumber++;
            var dataLine = await reader.ReadLineAsync().ConfigureAwait(false);
            if (dataLine == null) break;
            lineNumber++;

            // Only trim if necessary and avoid string allocations when possible
            var groupCode = ParseGroupCode(groupCodeLine);
            var isEntityWithType = groupCode == DxfCodeForType;

            // Create tag only if needed based on filters or hierarchy
            if (ShouldCreateTag(groupCode, dataLine, sectionStack.Count))
            {
                var tag = CreateTag(groupCode, dataLine.TrimEnd(), lineNumber - 1, groupCodeLine, dataLine);
                
                if (isEntityWithType)
                {
                    ProcessTypeTag(tag, dataLine.TrimEnd(), sections, sectionStack, ref currentTag);
                }
                else if (sectionStack.Count > 0)
                {
                    ProcessRegularTag(tag, sectionStack.Peek(), currentTag);
                }
                else
                {
                    sections.Add(tag);
                }
            }
        }

        return sections;
    }

    private static int ParseGroupCode(string line)
    {
        // Fast path for common single-digit group codes
        if (line.Length == 1 && line[0] >= '0' && line[0] <= '9')
        {
            return line[0] - '0';
        }

        // Use Span to avoid string allocations during parsing
        ReadOnlySpan<char> span = line.AsSpan().TrimEnd();
        return int.Parse(span);
    }

    private static bool ShouldCreateTag(int groupCode, string dataLine, int stackCount)
    {
        // Add your filtering logic here
        // For example, skip purely geometric data for visualization if not needed
        return true; // For now, accept all tags
    }

    private static DxfRawTag CreateTag(int groupCode, string dataElement, int lineNumber, 
        string originalGroupCodeLine, string originalDataLine)
    {
        return new DxfRawTag
        {
            GroupCode = groupCode,
            DataElement = dataElement,
            LineNumber = lineNumber,
            OriginalGroupCodeLine = originalGroupCodeLine,
            OriginalDataLine = originalDataLine,
            IsEnabled = true,
            Children = new List<DxfRawTag>() // Initialize only when needed
        };
    }

    private static void ProcessTypeTag(DxfRawTag tag, string dataElement, 
        List<DxfRawTag> sections, Stack<DxfRawTag> sectionStack, ref DxfRawTag? currentTag)
    {
        var isSectionStart = dataElement == DxfCodeNameSection;
        var isSectionEnd = dataElement == DxfCodeNameEndsec;

        if (isSectionStart)
        {
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
            tag.Parent = currentSection;
            currentSection.Children?.Add(tag);
            currentTag = tag;
        }
        else
        {
            sections.Add(tag);
        }
    }

    private static void ProcessRegularTag(DxfRawTag tag, DxfRawTag currentSection, DxfRawTag? currentTag)
    {
        if (currentTag != null)
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

    /// <summary>
    /// Parses a DXF file from a string (maintained for compatibility)
    /// Note: For large files, prefer ParseStreamAsync directly
    /// </summary>
    public static async Task<IList<DxfRawTag>> ParseAsync(string text)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(text).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
        stream.Position = 0;
        return await ParseStreamAsync(stream).ConfigureAwait(false);
    }
}
