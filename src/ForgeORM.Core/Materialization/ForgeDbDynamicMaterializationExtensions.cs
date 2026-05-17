using ForgeORM.Core.Materialization;

namespace ForgeORM.Core;

/// <summary>
/// User-friendly terminal materializers for raw SQL and dynamic result shapes.
/// These are intentionally simple: render/execute/read/materialize happens inside ForgeORM.
/// </summary>
public static class ForgeDbDynamicMaterializationExtensions
{
    /// <summary>
    /// Executes SQL and returns dictionary rows. Use this for pivots, dynamic reports and analytics projections.
    /// </summary>
    public static async Task<IReadOnlyList<Dictionary<string, object?>>> QueryDictionaryAsync(
        this ForgeDb db,
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = ForgeAdo.CreateCommand(
            connection,
            sql,
            parameters,
            transaction: null);

        return await ForgeDynamicRowReader.ReadDictionaryAsync(command, cancellationToken);
    }

    /// <summary>
    /// Executes SQL and returns a JSON projection object containing SQL, row count and dictionary rows.
    /// </summary>
    public static async Task<ForgeJsonProjection> QueryJsonProjectionAsync(
        this ForgeDb db,
        string sql,
        object? parameters = null,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.QueryDictionaryAsync(sql, parameters, cancellationToken);

        return new ForgeJsonProjection
        {
            Name = name,
            Sql = sql,
            RowCount = rows.Count,
            Rows = rows
        };
    }

    /// <summary>
    /// Executes SQL and returns a JSON string.
    /// </summary>
    public static async Task<string> QueryJsonAsync(
        this ForgeDb db,
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var projection = await db.QueryJsonProjectionAsync(sql, parameters, cancellationToken: cancellationToken);
        return ForgeMaterializationSerializer.ToJson(projection);
    }

    /// <summary>
    /// Executes SQL and returns a DataFrame-friendly tabular result.
    /// </summary>
    public static async Task<ForgeTabularResult> QueryDataFrameAsync(
        this ForgeDb db,
        string sql,
        object? parameters = null,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.QueryDictionaryAsync(sql, parameters, cancellationToken);

        return new ForgeTabularResult
        {
            Name = name,
            Columns = rows.SelectMany(x => x.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Rows = rows
        };
    }

    /// <summary>
    /// Executes SQL and returns CSV text.
    /// </summary>
    public static async Task<string> QueryCsvAsync(
        this ForgeDb db,
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.QueryDictionaryAsync(sql, parameters, cancellationToken);
        return ForgeMaterializationSerializer.ToCsv(rows);
    }
}
