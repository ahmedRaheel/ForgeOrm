using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

/// <summary>
/// Framework-level execution gateway used by all public ForgeORM APIs.
/// Provider-specific optimizations are selected behind <see cref="ForgePerformancePipeline"/>
/// and source-generated registries; public APIs must not branch to separate database frameworks.
/// </summary>
internal static class ForgeFrameworkExecutionPolicy
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DbConnection CreateConnection(IForgeDatabaseProvider provider, string connectionString)
        => provider.CreateConnection(connectionString);

    public static IReadOnlyList<T> Query<T>(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, int? timeoutSeconds, CommandType commandType = CommandType.Text)
    {
        using var connection = CreateConnection(provider, connectionString);
        return ForgePerformancePipeline.QueryAsync<T>(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds)
            .AsTask().GetAwaiter().GetResult();
    }

    public static async Task<IReadOnlyList<T>> QueryAsync<T>(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, int? timeoutSeconds, CancellationToken cancellationToken, CommandType commandType = CommandType.Text)
    {
        await using var connection = CreateConnection(provider, connectionString);
        return await ForgePerformancePipeline.QueryAsync<T>(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<IReadOnlyList<T>> QueryValueAsync<T>(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, int? timeoutSeconds, CancellationToken cancellationToken, CommandType commandType = CommandType.Text)
    {
        await using var connection = CreateConnection(provider, connectionString);
        return await ForgePerformancePipeline.QueryAsync<T>(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<IReadOnlyList<T>> QueryValueAsync<T, TParameters>(IForgeDatabaseProvider provider, string connectionString, string sql, TParameters parameters, int? timeoutSeconds, CancellationToken cancellationToken, CommandType commandType = CommandType.Text)
    {
        await using var connection = CreateConnection(provider, connectionString);
        return await ForgePerformancePipeline.QueryAsync<T, TParameters>(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public static T? FirstOrDefault<T>(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, int? timeoutSeconds, CommandType commandType = CommandType.Text)
    {
        using var connection = CreateConnection(provider, connectionString);
        return ForgePerformancePipeline.FirstOrDefaultAsync<T>(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds)
            .AsTask().GetAwaiter().GetResult();
    }

    public static async Task<T?> FirstOrDefaultAsync<T>(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, int? timeoutSeconds, CancellationToken cancellationToken, CommandType commandType = CommandType.Text)
    {
        await using var connection = CreateConnection(provider, connectionString);
        return await ForgePerformancePipeline.FirstOrDefaultAsync<T>(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public static T? SingleOrDefault<T>(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, int? timeoutSeconds, CommandType commandType = CommandType.Text)
    {
        using var connection = CreateConnection(provider, connectionString);
        return ForgePerformancePipeline.SingleOrDefaultAsync<T>(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds)
            .AsTask().GetAwaiter().GetResult();
    }

    public static async Task<T?> SingleOrDefaultAsync<T>(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, int? timeoutSeconds, CancellationToken cancellationToken, CommandType commandType = CommandType.Text)
    {
        await using var connection = CreateConnection(provider, connectionString);
        return await ForgePerformancePipeline.SingleOrDefaultAsync<T>(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public static int Execute(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, CommandType commandType, int? timeoutSeconds)
    {
        using var connection = CreateConnection(provider, connectionString);
        return ForgePerformancePipeline.ExecuteAsync(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds)
            .AsTask().GetAwaiter().GetResult();
    }

    public static async Task<int> ExecuteAsync(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, CommandType commandType, int? timeoutSeconds, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection(provider, connectionString);
        return await ForgePerformancePipeline.ExecuteAsync(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public static T? Scalar<T>(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, CommandType commandType, int? timeoutSeconds)
    {
        using var connection = CreateConnection(provider, connectionString);
        return ForgePerformancePipeline.ExecuteScalarAsync<T>(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds)
            .AsTask().GetAwaiter().GetResult();
    }

    public static async Task<T?> ScalarAsync<T>(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, CommandType commandType, int? timeoutSeconds, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection(provider, connectionString);
        return await ForgePerformancePipeline.ExecuteScalarAsync<T>(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<T?> ScalarValueAsync<T>(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, CommandType commandType, int? timeoutSeconds, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection(provider, connectionString);
        return await ForgePerformancePipeline.ExecuteScalarAsync<T>(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public static async IAsyncEnumerable<T> StreamAsync<T>(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, int? timeoutSeconds, [EnumeratorCancellation] CancellationToken cancellationToken, CommandType commandType = CommandType.Text)
    {
        await using var connection = CreateConnection(provider, connectionString);
        await foreach (var row in ForgePerformancePipeline.StreamAsync<T>(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            yield return row;
        }
    }

    public static async Task<IForgeGridReader> QueryMultipleAsync(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, int? timeoutSeconds, CancellationToken cancellationToken, CommandType commandType = CommandType.Text)
    {
        var connection = CreateConnection(provider, connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = ForgeAdo.CreateCommand(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(connection, command, await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false));
    }

    public static IForgeGridReader QueryMultiple(IForgeDatabaseProvider provider, string connectionString, string sql, object? parameters, int? timeoutSeconds, CommandType commandType = CommandType.Text)
    {
        var connection = CreateConnection(provider, connectionString);
        connection.Open();
        var command = ForgeAdo.CreateCommand(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(connection, command, command.ExecuteReader());
    }
}
