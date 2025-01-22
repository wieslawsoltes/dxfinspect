using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using dxfInspect.Model;

namespace dxfInspect.Services;

public static class DxfParser
{
    public const int DxfCodeForType = 0;
    public const string DxfCodeNameSection = "SECTION";
    public const string DxfCodeNameEndsec = "ENDSEC";

    private const int BufferSize = 4096;

    public class ParsingProgress
    {
        public long CurrentPosition { get; set; }
        public long TotalSize { get; set; }
        public double ProgressPercentage => TotalSize == 0 ? 0 : (double)CurrentPosition / TotalSize * 100;
        public string CurrentSection { get; set; } = string.Empty;
        public Exception? Error { get; set; }
    }

    public static async Task<IList<DxfRawTag>> ParseStreamAsync(Stream stream, IProgress<ParsingProgress>? progress = null)
    {
        var sections = new List<DxfRawTag>();
        var sectionStack = new Stack<DxfRawTag>();
        var currentTag = default(DxfRawTag);
        var parsingProgress = new ParsingProgress { TotalSize = stream.Length };

        try
        {
            using var reader = new StreamReader(stream, bufferSize: BufferSize);
            var lineNumber = 0;

            string? groupCodeLine;
            while ((groupCodeLine = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                var dataLine = await reader.ReadLineAsync();
                if (dataLine == null) break;
                lineNumber++;

                // Update progress
                parsingProgress.CurrentPosition = stream.Position;
                if (sectionStack.Count > 0 && sectionStack.Peek().DataElement != null)
                {
                    parsingProgress.CurrentSection = sectionStack.Peek().DataElement;
                }
                progress?.Report(parsingProgress);

                // Store original lines before any trimming
                var originalGroupCodeLine = groupCodeLine;
                var originalDataLine = dataLine;

                // Use spans for processing to avoid allocations
                var groupCodeSpan = groupCodeLine.AsSpan().Trim();
                var dataSpan = dataLine.AsSpan().Trim();

                var groupCode = ParseGroupCode(groupCodeSpan);
                var isEntityWithType = groupCode == DxfCodeForType;

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

            return sections;
        }
        catch (Exception ex)
        {
            parsingProgress.Error = ex;
            progress?.Report(parsingProgress);
            throw;
        }
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
