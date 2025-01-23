using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using dxfInspect.Model;

namespace dxfInspect.Services;

public class DxfRawTagCache
{
    private static readonly Lazy<DxfRawTagCache> _instance = new(() => new DxfRawTagCache());
    private readonly ConcurrentDictionary<string, WeakReference<DxfRawTag>> _tagCache = new();
    private int _referenceCount = 0;

    public static DxfRawTagCache Instance => _instance.Value;

    private DxfRawTagCache() { }

    public void IncrementReferenceCount()
    {
        System.Threading.Interlocked.Increment(ref _referenceCount);
    }

    public void DecrementReferenceCount()
    {
        if (System.Threading.Interlocked.Decrement(ref _referenceCount) == 0)
        {
            Clear(); // Clear cache when no more references exist
        }
    }

    public string GenerateKey(DxfRawTag tag)
    {
        return $"{tag.LineNumber}:{tag.GroupCode}:{tag.DataElement}";
    }

    public DxfRawTag GetOrCreate(DxfRawTag source, DxfRawTag? parent = null)
    {
        var key = GenerateKey(source);

        if (_tagCache.TryGetValue(key, out var weakRef) && 
            weakRef.TryGetTarget(out var cachedTag))
        {
            // Update parent reference if needed
            if (parent != null)
            {
                cachedTag.Parent = parent;
            }
            return cachedTag;
        }

        // Create new tag with minimal data
        var newTag = new DxfRawTag
        {
            GroupCode = source.GroupCode,
            DataElement = source.DataElement,
            LineNumber = source.LineNumber,
            OriginalGroupCodeLine = source.OriginalGroupCodeLine,
            OriginalDataLine = source.OriginalDataLine,
            Parent = parent,
            Children = new List<DxfRawTag>()
        };

        _tagCache.TryAdd(key, new WeakReference<DxfRawTag>(newTag));

        // Process children if they exist
        if (source.Children != null)
        {
            foreach (var child in source.Children)
            {
                var cachedChild = GetOrCreate(child, newTag);
                newTag.Children?.Add(cachedChild);
            }
        }

        return newTag;
    }

    private void Clear()
    {
        _tagCache.Clear();
    }

    public void CollectGarbage()
    {
        // Remove entries where WeakReference is no longer alive
        foreach (var key in _tagCache.Keys)
        {
            if (_tagCache.TryGetValue(key, out var weakRef) && 
                !weakRef.TryGetTarget(out _))
            {
                _tagCache.TryRemove(key, out _);
            }
        }
    }
}
