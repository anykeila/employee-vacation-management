using FluentAssertions;
using VacationManagement.Domain.ValueObjects;
using Xunit;

namespace VacationManagement.UnitTests.Domain;

public class DateRangeTests
{
    private static DateOnly D(int month, int day) => new(2026, month, day);

    [Fact]
    public void TotalDays_IsInclusive()
    {
        new DateRange(D(8, 1), D(8, 5)).TotalDays.Should().Be(5);
    }

    [Fact]
    public void TotalDays_SingleDay_IsOne()
    {
        new DateRange(D(8, 1), D(8, 1)).TotalDays.Should().Be(1);
    }

    [Fact]
    public void Constructor_EndBeforeStart_Throws()
    {
        var act = () => new DateRange(D(8, 5), D(8, 1));
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(8, 3, 8, 7)]   // partial overlap
    [InlineData(8, 1, 8, 5)]   // identical range
    [InlineData(8, 2, 8, 4)]   // fully contained
    [InlineData(7, 30, 8, 1)]  // touches on the start day
    [InlineData(8, 5, 8, 9)]   // touches on the end day
    public void Overlaps_WhenRangesShareAtLeastOneDay_ReturnsTrue(int sm, int sd, int em, int ed)
    {
        var reference = new DateRange(D(8, 1), D(8, 5));
        var other = new DateRange(D(sm, sd), D(em, ed));

        reference.Overlaps(other).Should().BeTrue();
        other.Overlaps(reference).Should().BeTrue();
    }

    [Theory]
    [InlineData(8, 6, 8, 10)]  // adjacent, the day after
    [InlineData(7, 25, 7, 31)] // adjacent, the day before
    [InlineData(9, 1, 9, 5)]   // far apart
    public void Overlaps_WhenRangesAreDisjoint_ReturnsFalse(int sm, int sd, int em, int ed)
    {
        var reference = new DateRange(D(8, 1), D(8, 5));
        var other = new DateRange(D(sm, sd), D(em, ed));

        reference.Overlaps(other).Should().BeFalse();
        other.Overlaps(reference).Should().BeFalse();
    }
}
