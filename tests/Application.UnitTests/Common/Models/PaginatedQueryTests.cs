using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Domain.Constants;
using NUnit.Framework;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Common.Models;

public class PaginatedQueryTests
{
    private sealed record Query : PaginatedQuery;

    private sealed record NarrowQuery : PaginatedQuery
    {
        protected override int MaxAllowedPageSize => 5;
    }

    [Test]
    public void ShouldFallBackToTheDefaultsWhenNothingIsAsked()
    {
        // A caller who sends no paging at all gets the first page, not an error. Before the
        // properties became nullable, minimal APIs rejected that request outright with a 500.
        var query = new Query();

        query.Page.ShouldBe(PagingDefaults.DefaultPageNumber);
        query.Size.ShouldBe(PagingDefaults.DefaultPageSize);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-9999)]
    public void ShouldClampANonsensePageToTheFirst(int pageNumber)
    {
        new Query { PageNumber = pageNumber }.Page.ShouldBe(PagingDefaults.DefaultPageNumber);
    }

    [Test]
    public void ShouldCapTheSizeAtTheUpperBound()
    {
        // Clamped rather than rejected: the caller gets as much as they may have, and no request
        // can make the server materialise an unbounded page.
        new Query { PageSize = 9_999 }.Size.ShouldBe(PagingDefaults.MaxPageSize);
    }

    [Test]
    public void ShouldHonourAQuerysOwnUpperBound()
    {
        new NarrowQuery { PageSize = 100 }.Size.ShouldBe(5);
        new NarrowQuery { PageSize = 3 }.Size.ShouldBe(3);
    }

    [Test]
    public void ShouldKeepAReasonableRequestAsAsked()
    {
        var query = new Query { PageNumber = 4, PageSize = 25 };

        query.Page.ShouldBe(4);
        query.Size.ShouldBe(25);
    }
}
