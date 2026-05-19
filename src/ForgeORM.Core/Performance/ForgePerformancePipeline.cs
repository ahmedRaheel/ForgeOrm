using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Central high-performance ADO.NET pipeline used by ForgeDb. It relies on MSIL parameter binders and MSIL reader materializers.
/// </summary>
public static class ForgePerformancePipeline
{
    public static async Task<IReadOnlyList<T>> QueryAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        _ = ForgeRuntimeQueryPlanCache.For<T>(sql, commandType, buffered: true);
        return await ForgeAdo.QueryAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async IAsyncEnumerable<T> StreamAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = ForgeRuntimeQueryPlanCache.For<T>(sql, commandType, buffered: false);

        await using var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection, cancellationToken)
            .ConfigureAwait(false);

        var materializer = ForgeIlMaterializerCache.GetOrCreate<T>(reader);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            yield return materializer(reader);
    }

    public static Task<T?> FirstOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        _ = ForgeRuntimeQueryPlanCache.For<T>(sql, commandType, buffered: true);
        return ForgeAdo.QueryFirstOrDefaultAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    public static Task<T?> SingleOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        _ = ForgeRuntimeQueryPlanCache.For<T>(sql, commandType, buffered: true);
        return ForgeAdo.QuerySingleOrDefaultAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    public static Task<int> ExecuteAsync(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        _ = ForgeRuntimeQueryPlanCache.For<int>(sql, commandType, buffered: true);
        return ForgeAdo.ExecuteAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    public static async Task<ForgePagedResult<T>> PageAsync<T>(
        DbConnection connection,
        IForgeDatabaseProvider provider,
        ForgePageRequest request,
        CancellationToken cancellationToken = default)
    {
        var count = provider.BuildCount(request.Sql, request.Parameters);
        var total = await ForgeAdo.ExecuteScalarAsync<int>(connection, count.CommandText, count.Parameters, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var page = provider.BuildPage(request);
        var rows = await QueryAsync<T>(connection, page.CommandText, page.Parameters, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new ForgePagedResult<T>
        {
            Items = rows,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = total
        };
    }
}
