namespace FotoTime.Domain.ValueObjects;

public sealed class Period : IEquatable<Period>
{
    private Period(DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start)
        {
            throw new ArgumentException("The end of a period must be greater than or equal to the start.", nameof(end));
        }

        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }

    public static Period Create(DateTimeOffset start, DateTimeOffset end) => new(start, end);

    public bool Contains(DateTimeOffset timestamp) => timestamp >= Start && timestamp <= End;

    public bool Overlaps(Period other) => Start <= other.End && End >= other.Start;

    public void EnsureDoesNotOverlap(IEnumerable<Period> others)
    {
        foreach (var period in others)
        {
            if (Overlaps(period))
            {
                throw new InvalidOperationException("Periods may not overlap.");
            }
        }
    }

    public bool Equals(Period? other) => other is not null && Start.Equals(other.Start) && End.Equals(other.End);

    public override bool Equals(object? obj) => obj is Period other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public override string ToString() => $"{Start:u}–{End:u}";
}
