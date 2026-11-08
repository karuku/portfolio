using Dapper;
using Npgsql;

namespace DatabaseApi;

public sealed class OrderRepository(NpgsqlDataSource dataSource)
{
    private const string FirstPageQuery = """
        SELECT id AS "Id",
               customer_id AS "CustomerId",
               created_at AS "CreatedAt",
               status AS "Status",
               total_cents AS "TotalCents"
        FROM orders
        WHERE customer_id = @CustomerId
        ORDER BY created_at DESC, id DESC
        LIMIT @Take
        """;

    private const string NextPageQuery = """
        SELECT id AS "Id",
               customer_id AS "CustomerId",
               created_at AS "CreatedAt",
               status AS "Status",
               total_cents AS "TotalCents"
        FROM orders
        WHERE customer_id = @CustomerId
          AND (created_at, id) < (@BeforeCreatedAt, @BeforeId)
        ORDER BY created_at DESC, id DESC
        LIMIT @Take
        """;

    public async Task<OrderPageResponse> ListAsync(OrderPageQuery query, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            query.BeforeCreatedAt.HasValue ? NextPageQuery : FirstPageQuery,
            new
            {
                query.CustomerId,
                query.BeforeCreatedAt,
                query.BeforeId,
                Take = query.PageSize + 1
            },
            cancellationToken: cancellationToken);

        var rows = (await connection.QueryAsync<OrderRow>(command)).AsList();
        var hasMore = rows.Count > query.PageSize;
        var page = rows.Take(query.PageSize).ToArray();
        var last = page.LastOrDefault();
        var nextCursor = hasMore && last is not null
            ? new OrderCursor(last.CreatedAt, last.Id)
            : null;

        return new OrderPageResponse(page, query.PageSize, nextCursor);
    }
}
