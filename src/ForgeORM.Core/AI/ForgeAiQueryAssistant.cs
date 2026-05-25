namespace ForgeORM.Core.AI;

public static class ForgeAiQueryAssistant
{
    public static ForgeAiSqlResponse GenerateSql(ForgeAiSqlRequest request)
    {
        var prompt = request.Prompt.ToLowerInvariant();
        var table = string.IsNullOrWhiteSpace(request.Entity) ? "Orders" : request.Entity;

        string sql;

        if (prompt.Contains("top") && prompt.Contains("customer"))
        {
            sql = $"SELECT TOP (10) CustomerId, SUM(GrandTotal) AS Revenue FROM {table} GROUP BY CustomerId ORDER BY Revenue DESC";
        }
        else if (prompt.Contains("monthly") || prompt.Contains("month"))
        {
            sql = $"SELECT YEAR(CreatedAt) AS [Year], MONTH(CreatedAt) AS [Month], SUM(GrandTotal) AS Revenue FROM {table} GROUP BY YEAR(CreatedAt), MONTH(CreatedAt)";
        }
        else if (prompt.Contains("count"))
        {
            sql = $"SELECT COUNT(1) AS Total FROM {table}";
        }
        else
        {
            sql = $"SELECT TOP (100) * FROM {table}";
        }

        return new ForgeAiSqlResponse(
            sql,
            request.SafeMode && sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase) ? ["AI generated SELECT *. Review projection before production use."] : [],
            ["Review WHERE + ORDER BY columns for index strategy."],
            [$"Generated from prompt: {request.Prompt}", "This deterministic fallback can be replaced by an LLM provider."]);
    }

    public static ForgeAiSchemaInsight AnalyzeSchema(string entity, IReadOnlyList<string> columns)
    {
        return new ForgeAiSchemaInsight(
            entity,
            [$"{entity}ListDto", $"{entity}DetailDto", $"Create{entity}Request", $"Update{entity}Request"],
            [$"GET /{entity.ToLowerInvariant()}s", $"GET /{entity.ToLowerInvariant()}s/{{id}}", $"POST /{entity.ToLowerInvariant()}s"],
            columns.Where(c => c.EndsWith("Id", StringComparison.OrdinalIgnoreCase) || c.Contains("Created", StringComparison.OrdinalIgnoreCase))
                .Select(c => $"IX_{entity}_{c}")
                .ToList());
    }
}
