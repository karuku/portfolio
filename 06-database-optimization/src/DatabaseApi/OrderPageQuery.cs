namespace DatabaseApi;

public sealed record OrderCursor(DateTimeOffset CreatedAt, Guid Id);

public sealed record OrderRow(
    Guid Id,
    Guid CustomerId,
    DateTimeOffset CreatedAt,
    string Status,
    long TotalCents);

public sealed record OrderPageResponse(
    IReadOnlyList<OrderRow> Items,
    int PageSize,
    OrderCursor? NextCursor);

public sealed record OrderPageQuery(
    Guid CustomerId,
    int PageSize,
    DateTimeOffset? BeforeCreatedAt,
    Guid? BeforeId)
{
    public const int DefaultPageSize = 25;
    public const int MaximumPageSize = 100;

    public static bool TryCreate(
        Guid customerId,
        int? pageSize,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        out OrderPageQuery query)
    {
        var resolvedPageSize = pageSize ?? DefaultPageSize;
        if (customerId == Guid.Empty
            || resolvedPageSize is < 1 or > MaximumPageSize
            || beforeCreatedAt.HasValue != beforeId.HasValue)
        {
            query = default!;
            return false;
        }

        query = new OrderPageQuery(customerId, resolvedPageSize, beforeCreatedAt, beforeId);
        return true;
    }
}
