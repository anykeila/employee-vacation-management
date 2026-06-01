using FluentAssertions;
using VacationManagement.Application.Common;
using Xunit;

namespace VacationManagement.UnitTests.Common;

public class PaginationQueryTests
{
    [Fact]
    public void Defaults_ArePageOneAndTwentyPerPage()
    {
        var query = new PaginationQuery();

        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
        query.Skip.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Page_BelowOne_IsCoercedToOne(int value)
    {
        new PaginationQuery { Page = value }.Page.Should().Be(1);
    }

    [Fact]
    public void PageSize_AboveMax_IsClampedToMax()
    {
        new PaginationQuery { PageSize = 5000 }.PageSize.Should().Be(PaginationQuery.MaxPageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageSize_BelowOne_FallsBackToDefault(int value)
    {
        new PaginationQuery { PageSize = value }.PageSize.Should().Be(20);
    }

    [Fact]
    public void Skip_ReflectsPageAndSize()
    {
        new PaginationQuery { Page = 3, PageSize = 25 }.Skip.Should().Be(50);
    }

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(45, 20, 3)]
    public void TotalPages_RoundsUp(int totalCount, int pageSize, int expected)
    {
        var result = new PagedResult<int>(Array.Empty<int>(), 1, pageSize, totalCount);
        result.TotalPages.Should().Be(expected);
    }
}
