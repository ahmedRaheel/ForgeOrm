using System.Text;
using ForgeORM.DataFrame;

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException("Regression failed: " + message);
}

static object? Get(IReadOnlyDictionary<string, object?> row, string column)
    => row.TryGetValue(column, out var value) ? value : null;

await TestCsvNullLikeValuesAsync();
await TestJsonNullLikeValuesAsync();
await TestStreamOverloadsAsync();
await TestNumericHeadersAndDataFrameBridgeAsync();

Console.WriteLine("ForgeORM regression checks passed.");

static async Task TestCsvNullLikeValuesAsync()
{
    var csv = "Name,Price,1980\nLaptop,?,10\nMouse,120.50,?";
    var frame = ForgeDataFrame.FromCsvText(csv);

    Assert(frame.RowCount == 2, "CSV row count should be 2.");
    Assert(Get(frame.Rows[0], "Price") is null, "CSV '?' must become null.");
    Assert(Get(frame.Rows[1], "1980") is null, "CSV numeric-header '?' must become null.");
}

static async Task TestJsonNullLikeValuesAsync()
{
    var json = """
    [
      { "Name": "Laptop", "Price": "?", "Stock": "NA" },
      { "Name": "Mouse", "Price": 120.50, "Stock": 10 }
    ]
    """;

    var frame = ForgeDataFrame.FromJsonText(json);

    Assert(frame.RowCount == 2, "JSON row count should be 2.");
    Assert(Get(frame.Rows[0], "Price") is null, "JSON '?' must become null.");
    Assert(Get(frame.Rows[0], "Stock") is null, "JSON 'NA' must become null.");
}

static async Task TestStreamOverloadsAsync()
{
    await using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes("A,B\n1,?"));
    var csvFrame = await ForgeDataFrame.FromCsvAsync(csvStream);
    Assert(Get(csvFrame.Rows[0], "B") is null, "CSV stream overload should normalize '?'.");

    await using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes("[{\"A\":1,\"B\":\"?\"}]"));
    var jsonFrame = await ForgeDataFrame.FromJsonAsync(jsonStream);
    Assert(Get(jsonFrame.Rows[0], "B") is null, "JSON stream overload should normalize '?'.");
}

static async Task TestNumericHeadersAndDataFrameBridgeAsync()
{
    var frame = ForgeDataFrame.FromCsvText("Country,1980,1981\nCanada,10,?\nPakistan,20,30");
    Assert(frame.Columns.Contains("1980"), "Numeric CSV header 1980 should be preserved.");
    Assert(Get(frame.Rows[0], "1981") is null, "Numeric column null-like value should be null.");

    var microsoftFrame = frame.ToMicrosoftDataFrame();
    Assert(microsoftFrame.Columns.Any(c => c.Name == "1980"), "Microsoft.Data.Analysis bridge should keep numeric headers.");
}
