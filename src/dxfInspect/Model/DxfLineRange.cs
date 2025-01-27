using System;

namespace dxfInspect.Model;

public readonly struct DxfLineRange : IComparable<DxfLineRange>, IEquatable<DxfLineRange>
{
    public int Start { get; }
    public int End { get; }

    public DxfLineRange(int start, int end)
    {
        if (end < start)
        {
            throw new ArgumentException("End line must be greater than or equal to start line", nameof(end));
        }
            
        Start = start;
        End = end;
    }

    public override string ToString() => $"{Start}-{End}";

    public int CompareTo(DxfLineRange other)
    {
        var startComparison = Start.CompareTo(other.Start);
        return startComparison != 0 ? startComparison : End.CompareTo(other.End);
    }

    public bool Equals(DxfLineRange other) => Start == other.Start && End == other.End;

    public override bool Equals(object? obj) => obj is DxfLineRange other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(DxfLineRange left, DxfLineRange right) => left.Equals(right);

    public static bool operator !=(DxfLineRange left, DxfLineRange right) => !left.Equals(right);

    public static bool operator <(DxfLineRange left, DxfLineRange right) => left.CompareTo(right) < 0;

    public static bool operator <=(DxfLineRange left, DxfLineRange right) => left.CompareTo(right) <= 0;

    public static bool operator >(DxfLineRange left, DxfLineRange right) => left.CompareTo(right) > 0;

    public static bool operator >=(DxfLineRange left, DxfLineRange right) => left.CompareTo(right) >= 0;
}
