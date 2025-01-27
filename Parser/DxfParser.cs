using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Text;

namespace SimdDxfParser;

public readonly record struct DxfTag(int Code, string Value);

public sealed class DxfParser : IDisposable
{
    private MemoryMappedFile? _mappedFile;
    private MemoryMappedViewAccessor? _accessor;
    private bool _isDisposed;

    public IEnumerable<DxfTag> ParseFile(string filePath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(DxfParser));

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("DXF file not found", filePath);

        // Create memory mapped file
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
            // Read group code
            var codeLine = reader.ReadLine();
            if (codeLine.IsEmpty) continue;

            // Parse group code
            if (!int.TryParse(codeLine, out var groupCode)) continue;

            // Read value - can be empty after valid code
            var valueLine = reader.ReadLine();
            if (valueLine.IsEmpty)
            {
                tags.Add(new DxfTag(groupCode, string.Empty));
                continue;
            }

            var value = ProcessValue(valueLine);
            tags.Add(new DxfTag(groupCode, value));
        }

        return tags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ProcessValue(ReadOnlySpan<byte> valueBytes)
    {
        // Fast path for empty values
        if (valueBytes.IsEmpty) return string.Empty;

        // Convert to string and trim
        return Encoding.ASCII.GetString(valueBytes).Trim();
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _accessor?.Dispose();
        _mappedFile?.Dispose();
        _isDisposed = true;
    }

    // Helper struct for efficient span-based line reading
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
            var span = _remaining;
            var newLineIndex = span.IndexOf((byte)'\n');

            if (newLineIndex == -1)
            {
                _remaining = ReadOnlySpan<byte>.Empty;
                return TrimEndBytes(span);
            }

            var line = span[..newLineIndex];
            _remaining = span[(newLineIndex + 1)..];

            // Handle \r\n
            if (line.Length > 0 && line[^1] == '\r')
            {
                line = line[..^1];
            }

            return TrimEndBytes(line);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> TrimEndBytes(ReadOnlySpan<byte> span)
        {
            var length = span.Length;
            while (length > 0 && span[length - 1] <= 32)
            {
                length--;
            }
            return span[..length];
        }
    }
}
