using System;

namespace dxfInspect.Model;

public readonly struct LineRange : IComparable<LineRange>, IEquatable<LineRange>
{
    public int Start { get; }
    public int End { get; }

    public LineRange(int start, int end)
    {
        if (end < start)
        {
            throw new ArgumentException("End line must be greater than or equal to start line", nameof(end));
        }
            
        Start = start;
        End = end;
    }

    public override string ToString() => $"{Start}-{End}";

    public int CompareTo(LineRange other)
    {
        var startComparison = Start.CompareTo(other.Start);
        return startComparison != 0 ? startComparison : End.CompareTo(other.End);
    }

    public bool Equals(LineRange other) => Start == other.Start && End == other.End;

    public override bool Equals(object? obj) => obj is LineRange other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(LineRange left, LineRange right) => left.Equals(right);

    public static bool operator !=(LineRange left, LineRange right) => !left.Equals(right);

    public static bool operator <(LineRange left, LineRange right) => left.CompareTo(right) < 0;

    public static bool operator <=(LineRange left, LineRange right) => left.CompareTo(right) <= 0;

    public static bool operator >(LineRange left, LineRange right) => left.CompareTo(right) > 0;

    public static bool operator >=(LineRange left, LineRange right) => left.CompareTo(right) >= 0;
}
