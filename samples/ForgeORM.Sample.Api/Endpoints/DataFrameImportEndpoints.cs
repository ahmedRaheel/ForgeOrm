using ForgeORM.Core;
using ForgeORM.DataFrame;
using ForgeORM.SchemaOps;

public static class DataFrameImportEndpoints
{
    public static IEndpointRouteBuilder MapDataFrameImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dataframes").WithTags("12 DataFrame Import / Export / Tables");

        group.MapPost("/import/csv/orders/to-table", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var csv = """
            OrderNo,CustomerId,Status,GrandTotal,CreatedAt
            CSV-1001,1,Paid,1250.75,2026-05-01T10:00:00+05:00
            CSV-1002,1,Draft,540.25,2026-05-02T12:00:00+05:00
            CSV-1003,2,Cancelled,300.00,2026-05-03T14:00:00+05:00
            """;

            var frame = ForgeDataFrame.FromCsvText(csv);

            await frame.ToTableAsync(
                db,
                tableName: "dbo.ForgeImportedOrderCsvFrame",
                createIfNotExists: true,
                dropIfExists: true,
                cancellationToken: ct);

            var queried = await db.Frame<Order>()
                .FromSql("SELECT * FROM dbo.ForgeImportedOrderCsvFrame")
                .ToFrameAsync(ct);

            var summary = queried.GroupBy("Status")
                .Agg(
                    ForgeAggregation.Count(alias: "Orders"),
                    ForgeAggregation.Sum("GrandTotal", "Total"),
                    ForgeAggregation.Avg("GrandTotal", "Average"));

            return Results.Ok(new
            {
                importedRows = frame.RowCount,
                table = "dbo.ForgeImportedOrderCsvFrame",
                rows = queried.Rows,
                summary = summary.Rows
            });
        });

        group.MapPost("/import/json/orders/to-table", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var json = """
            [
              { "OrderNo": "JSON-1001", "CustomerId": 1, "Status": "Paid", "GrandTotal": 900.50, "CreatedAt": "2026-05-04T09:00:00+05:00" },
              { "OrderNo": "JSON-1002", "CustomerId": 2, "Status": "Draft", "GrandTotal": 150.00, "CreatedAt": "2026-05-05T11:30:00+05:00" },
              { "OrderNo": "JSON-1003", "CustomerId": 2, "Status": "Paid", "GrandTotal": 2200.00, "CreatedAt": "2026-05-06T15:45:00+05:00" }
            ]
            """;

            var frame = ForgeDataFrame.FromJsonText(json);

            await frame.ToTableAsync(
                db,
                tableName: "dbo.ForgeImportedOrderJsonFrame",
                createIfNotExists: true,
                dropIfExists: true,
                cancellationToken: ct);

            var queried = await db.Frame<Order>()
                .FromSql("SELECT * FROM dbo.ForgeImportedOrderJsonFrame")
                .ToFrameAsync(ct);

            var pivot = queried.PivotTable(
                rows: "CustomerId",
                columns: "Status",
                values: "GrandTotal",
                aggregate: ForgeAgg.Sum());

            return Results.Ok(new
            {
                importedRows = frame.RowCount,
                table = "dbo.ForgeImportedOrderJsonFrame",
                rows = queried.Rows,
                pivot = pivot.Rows
            });
        });

        group.MapGet("/imported-orders/query", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var exists = await db.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(1)
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = 'dbo'
                  AND TABLE_NAME = 'ForgeImportedOrderJsonFrame'
                """,
                cancellationToken: ct);

            if (exists == 0)
            {
                return Results.NotFound(new
                {
                    message = "Import table does not exist yet.",
                    requiredStep = "Call POST /dataframes/import/json/orders/to-table first.",
                    table = "dbo.ForgeImportedOrderJsonFrame"
                });
            }

            var frame = await db.Frame<dynamic>()
                .From("dbo.ForgeImportedOrderJsonFrame")
                .ToFrameAsync(ct);

            return Results.Ok(new
            {
                rows = frame.RowCount,
                columns = frame.Columns,
                data = frame.Rows
            });
        });

        group.MapPost("/import/csv-to-table", async (
            IFormFile file,
            string tableName,
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return Results.BadRequest("tableName is required.");
            }

            if (!ForgeSqlNameValidator.IsSafeIdentifier(tableName))
            {
                return Results.BadRequest("Invalid tableName.");
            }

            await using var stream = file.OpenReadStream();
            var frame = await ForgeDataFrame.FromCsvAsync(stream, ct);

            await frame.ToTableAsync(
                db,
                tableName: tableName,
                cancellationToken: ct);

            return Results.Ok(new
            {
                message = "CSV imported successfully.",
                tableName,
                file.FileName,
                rows = frame.RowCount,
                columns = frame.Columns
            });
        })
        .DisableAntiforgery();

        return app;
    }
}
