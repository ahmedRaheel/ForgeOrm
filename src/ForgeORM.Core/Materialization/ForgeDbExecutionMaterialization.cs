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
    /// Executes SQL and returns a JSON-friendly projection.
    /// </summary>
    public async ValueTask<ForgeJsonProjection> QueryJsonProjectionAsync(
        string sql,
        object? parameters = null,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryDictionaryAsync(
            sql,
            parameters,
            cancellationToken: cancellationToken);

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
    public async ValueTask<string> QueryJsonAsync(
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
    public async ValueTask<ForgeTabularResult> QueryDataFrameAsync(
        string sql,
        object? parameters = null,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryDictionaryAsync(
            sql,
            parameters,
            cancellationToken: cancellationToken);

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
    public async ValueTask<string> QueryCsvAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryDictionaryAsync(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        return ForgeMaterializationSerializer.ToCsv(rows);
    }
}
