using DatabaseApi;

namespace DatabaseApi.Tests;

public sealed class OrderPageQueryTests
{
    private static readonly Guid CustomerId = Guid.Parse("11111111-1111-4111-8111-111111111111");

    [Fact]
    public void Defaults_to_a_bounded_first_page()
    {
        var isValid = OrderPageQuery.TryCreate(CustomerId, null, null, null, out var query);

        Assert.True(isValid);
        Assert.Equal(OrderPageQuery.DefaultPageSize, query.PageSize);
        Assert.Null(query.BeforeCreatedAt);
        Assert.Null(query.BeforeId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Rejects_page_sizes_outside_the_supported_range(int pageSize)
    {
        var isValid = OrderPageQuery.TryCreate(CustomerId, pageSize, null, null, out _);

        Assert.False(isValid);
    }

    [Fact]
    public void Requires_both_cursor_values_or_neither()
    {
        var isValid = OrderPageQuery.TryCreate(CustomerId, 25, DateTimeOffset.UtcNow, null, out _);

        Assert.False(isValid);
    }

    [Fact]
    public void Rejects_empty_customer_ids()
    {
        var isValid = OrderPageQuery.TryCreate(Guid.Empty, 25, null, null, out _);

        Assert.False(isValid);
    }
}
