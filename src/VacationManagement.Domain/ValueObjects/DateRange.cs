namespace VacationManagement.Domain.ValueObjects;

/// <summary>
/// Inclusive date range [Start, End]. Both endpoints count as vacation days,
/// e.g. 01/08..05/08 spans 5 days.
/// </summary>
public readonly record struct DateRange
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentException(
                $"End date {end:O} cannot be earlier than start date {start:O}.", nameof(end));
        }

        Start = start;
        End = end;
    }

    public int TotalDays => End.DayNumber - Start.DayNumber + 1;

    /// <summary>
    /// Two inclusive ranges overlap when each starts on or before the other ends.
    /// Adjacent ranges (e.g. ..09-12 and 09-13..) do NOT overlap.
    /// </summary>
    public bool Overlaps(DateRange other) => Start <= other.End && other.Start <= End;
}
