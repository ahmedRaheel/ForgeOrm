using System.Data.Common;
using ForgeORM.Core.Materialization;

namespace ForgeORM.Core;

/// <summary>
/// Package-level dynamic materialization methods.
/// These keep CreateConnection private and execute through ForgeDb public query APIs when possible.
/// </summary>
public partial class ForgeDb
{
    /// <summary>
    /// Executes SQL and returns dictionary rows. Use this for pivots, reports, DataFrames and dynamic analytics projections.
    /// </summary>
    public async Task<IReadOnlyList<Dictionary<string, object?>>> QueryDictionaryPivotAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = ForgeAdo.CreateCommand(
            connection,
            sql,
            parameters,
            transaction: null);

        return await ForgeDynamicRowReader.ReadDictionaryAsync(
            command,
            cancellationToken);
    }

    /// <summary>
    /// Executes SQL and returns a JSON-friendly projection.
    /// </summary>
    public async Task<ForgeJsonProjection> QueryJsonProjectionAsync(
        string sql,
        object? parameters = null,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryDictionaryPivotAsync(
            sql,
            parameters,
            cancellationToken);

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
    public async Task<string> QueryJsonAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var projection = await QueryJsonProjectionAsync(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        return ForgeMaterializationSerializer.ToJson(projection);
    }

    /// <summary>
    /// Executes SQL and returns a DataFrame-friendly tabular result.
    /// </summary>
    public async Task<ForgeTabularResult> QueryDataFrameAsync(
        string sql,
        object? parameters = null,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryDictionaryPivotAsync(
            sql,
            parameters,
            cancellationToken);

        return new ForgeTabularResult
        {
            Name = name,
            Columns = rows
                .SelectMany(x => x.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Rows = rows
        };
    }

    /// <summary>
    /// Executes SQL and returns CSV text.
    /// </summary>
    public async Task<string> QueryCsvAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryDictionaryPivotAsync(
            sql,
            parameters,
            cancellationToken);

        return ForgeMaterializationSerializer.ToCsv(rows);
    }
}
