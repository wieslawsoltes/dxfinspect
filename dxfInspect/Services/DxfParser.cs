using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
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
        var lineNumber = 0;

        string? groupCodeLine;
        while ((groupCodeLine = reader.ReadLine()) != null)
        {
            lineNumber++;
            var dataLine = reader.ReadLine();
            if (dataLine == null) break;
            lineNumber++;

            // Store original lines before any trimming
            var originalGroupCodeLine = groupCodeLine;
            var originalDataLine = dataLine;

            // Use spans for processing to avoid allocations
            var groupCodeSpan = groupCodeLine.AsSpan().Trim();
            var dataSpan = dataLine.AsSpan().Trim();

            var groupCode = ParseGroupCode(groupCodeSpan);
            var isEntityWithType = groupCode == DxfCodeForType;

            if (ShouldCreateTag(groupCode, dataSpan))
            {
                var tag = CreateTag(groupCode, dataSpan.ToString(), lineNumber - 1, 
                    originalGroupCodeLine, originalDataLine);

                if (isEntityWithType)
                {
                    ProcessTypeTag(tag, dataSpan, sections, sectionStack, ref currentTag);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ParseGroupCode(ReadOnlySpan<char> span)
    {
        // Fast path for common single-digit group codes
        if (span.Length == 1 && span[0] >= '0' && span[0] <= '9')
        {
            return span[0] - '0';
        }

        return int.Parse(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldCreateTag(int groupCode, ReadOnlySpan<char> dataLine)
    {
        return true; // Accept all tags for now
    }

    private static bool MatchesSpan(ReadOnlySpan<char> span, string value)
    {
        return span.Equals(value.AsSpan(), StringComparison.Ordinal);
    }

    private static void ProcessTypeTag(DxfRawTag tag, ReadOnlySpan<char> dataElement,
        List<DxfRawTag> sections, Stack<DxfRawTag> sectionStack, ref DxfRawTag? currentTag)
    {
        var isSectionStart = MatchesSpan(dataElement, DxfCodeNameSection);
        var isSectionEnd = MatchesSpan(dataElement, DxfCodeNameEndsec);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            Children = new List<DxfRawTag>()
        };
    }
}
