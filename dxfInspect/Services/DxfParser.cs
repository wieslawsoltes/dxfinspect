using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using dxfInspect.Model;

namespace dxfInspect.Services;

public static class DxfParser
{
    public const int DxfCodeForType = 0;
    public const int DxfCodeForName = 2;
    public const string DxfCodeNameSection = "SECTION";
    public const string DxfCodeNameEndsec = "ENDSEC";

    private const int BufferSize = 4096;

    public static IList<DxfRawTag> ParseStream(Stream stream)
    {
        var sections = new List<DxfRawTag>();
        var sectionStack = new Stack<DxfRawTag>();
        var currentTag = default(DxfRawTag);

        using var reader = new StreamReader(stream, bufferSize: BufferSize);
        int lineNumber = 0;

        string? groupCodeLine;
        while ((groupCodeLine = reader.ReadLine()) != null)
        {
            lineNumber++;
            var dataLine = reader.ReadLine();
            if (dataLine == null) break;
            lineNumber++;

            // Store original lines before any trimming
            string originalGroupCodeLine = groupCodeLine;
            string originalDataLine = dataLine;

            // Fully trim both group code and data lines
            groupCodeLine = groupCodeLine.Trim();
            dataLine = dataLine.Trim(); // Trim both ends as DXF doesn't use significant whitespace

            var groupCode = ParseGroupCode(groupCodeLine);
            var isEntityWithType = groupCode == DxfCodeForType;

            if (ShouldCreateTag(groupCode, dataLine, sectionStack.Count))
            {
                var tag = CreateTag(groupCode, dataLine, lineNumber - 1, originalGroupCodeLine, originalDataLine);
                
                if (isEntityWithType)
                {
                    ProcessTypeTag(tag, dataLine, sections, sectionStack, ref currentTag);
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
        ReadOnlySpan<char> span = line.AsSpan().Trim();
        return int.Parse(span);
    }

    private static bool ShouldCreateTag(int groupCode, string dataLine, int stackCount)
    {
        return true; // Accept all tags for now
    }

    private static DxfRawTag CreateTag(int groupCode, string dataElement, int lineNumber, 
        string originalGroupCodeLine, string originalDataLine)
    {
        return new DxfRawTag
        {
            GroupCode = groupCode,
            DataElement = dataElement, // Fully trimmed
            LineNumber = lineNumber,
            OriginalGroupCodeLine = originalGroupCodeLine,
            OriginalDataLine = originalDataLine,
            IsEnabled = true,
            Children = new List<DxfRawTag>()
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
}
