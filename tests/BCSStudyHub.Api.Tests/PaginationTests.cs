using BCSStudyHub.Application.Common;

namespace BCSStudyHub.Api.Tests;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(0, 0, 1, 1, 0)]
    [InlineData(3, 25, 3, 25, 50)]
    [InlineData(2, 500, 2, 100, 100)]
    [InlineData(int.MaxValue, 100, int.MaxValue, 100, int.MaxValue)]
    public void PageRequestNormalizesBounds(
        int pageNumber,
        int pageSize,
        int expectedPageNumber,
        int expectedPageSize,
        int expectedOffset)
    {
        var request = new PageRequest(pageNumber, pageSize);

        Assert.Equal(expectedPageNumber, request.EffectivePageNumber);
        Assert.Equal(expectedPageSize, request.EffectivePageSize);
        Assert.Equal(expectedOffset, request.Offset);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    public void PagedResponseCalculatesPageCount(long totalCount, long expectedPages)
    {
        var response = new PagedResponse<string>([], 1, 20, totalCount);

        Assert.Equal(expectedPages, response.TotalPages);
    }
}
