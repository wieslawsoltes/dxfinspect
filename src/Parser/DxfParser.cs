using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Text;

namespace Parser;

public readonly record struct DxfTag(int Code, string Value);

public sealed class DxfParser : IDisposable
{
    private MemoryMappedFile? _mappedFile;
    private MemoryMappedViewAccessor? _accessor;
    private bool _isDisposed;

    public List<DxfTag> ParseFile(string filePath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(DxfParser));

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("DXF file not found", filePath);

        // Create memory-mapped file
        _mappedFile = MemoryMappedFile.CreateFromFile(
            filePath,
            FileMode.Open,
            mapName: null,
            fileInfo.Length,
            MemoryMappedFileAccess.Read);

        _accessor = _mappedFile.CreateViewAccessor(0, fileInfo.Length, MemoryMappedFileAccess.Read);

        unsafe
        {
            byte* ptr = null;
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            try
            {
                var span = new ReadOnlySpan<byte>(ptr, (int)fileInfo.Length);
                return ParseBytes(span);
            }
            finally
            {
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }
    }

    private List<DxfTag> ParseBytes(ReadOnlySpan<byte> bytes)
    {
        var tags = new List<DxfTag>();
        var reader = new SpanReader(bytes);

        while (!reader.IsEmpty)
        {
            // Read group code line
            var codeBytes = reader.ReadLine();
            if (codeBytes.IsEmpty)
                continue;

            // Parse group code as int (no negative allowed)
            if (!TryParseAsciiInt(codeBytes, out int groupCode))
                continue;

            // Read value line (may be empty)
            var valueBytes = reader.ReadLine();
            string value = valueBytes.IsEmpty 
                ? string.Empty 
                : Encoding.ASCII.GetString(valueBytes);

            tags.Add(new DxfTag(groupCode, value));
        }

        return tags;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _accessor?.Dispose();
        _mappedFile?.Dispose();
        _isDisposed = true;
    }

    /// <summary>
    /// Tries to parse an integer from the given ASCII <paramref name="span"/>.
    /// Assumes the code is always >= 0 (no sign check).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseAsciiInt(ReadOnlySpan<byte> span, out int value)
    {
        value = 0;
        long result = 0;
        int i = 0;

        // Skip leading whitespaces
        while (i < span.Length && span[i] <= 32)
            i++;

        // If we have nothing left after whitespace, fail
        if (i >= span.Length)
            return false;

        // Parse digits
        for (; i < span.Length; i++)
        {
            byte b = span[i];
            if (b < '0' || b > '9')
                break;

            result = result * 10 + (b - '0');
            // Could check for overflow if desired
            if (result > int.MaxValue)
                return false;
        }

        value = (int)result;
        return true;
    }

    /// <summary>
    /// Reads lines from a byte span, returning trimmed slices (leading + trailing whitespace removed).
    /// </summary>
    private ref struct SpanReader
    {
        private ReadOnlySpan<byte> _remaining;

        public SpanReader(ReadOnlySpan<byte> data)
        {
            _remaining = data;
        }

        public bool IsEmpty => _remaining.IsEmpty;

        public ReadOnlySpan<byte> ReadLine()
        {
            if (_remaining.IsEmpty)
                return ReadOnlySpan<byte>.Empty;

            ReadOnlySpan<byte> span = _remaining;
            int newLineIndex = span.IndexOf((byte)'\n');

            if (newLineIndex == -1)
            {
                // No newline left: return entire remainder
                _remaining = ReadOnlySpan<byte>.Empty;
                return TrimBytes(span);
            }

            // Extract line
            var line = span[..newLineIndex];
            _remaining = span[(newLineIndex + 1)..];

            // Handle \r\n
            if (line.Length > 0 && line[^1] == '\r')
            {
                line = line[..^1];
            }

            return TrimBytes(line);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> TrimBytes(ReadOnlySpan<byte> span)
        {
            // Trim leading
            int start = 0;
            while (start < span.Length && span[start] <= 32)
                start++;

            // Trim trailing
            int end = span.Length - 1;
            while (end >= start && span[end] <= 32)
                end--;

            if (end < start)
                return ReadOnlySpan<byte>.Empty;

            return span[start..(end + 1)];
        }
    }
}
