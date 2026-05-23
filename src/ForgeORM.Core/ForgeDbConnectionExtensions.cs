using System.Data;
using System.Data.Common;
using ForgeORM.Abstractions;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

/// <summary>
/// Dapper-style extension methods owned by ForgeORM.
/// These APIs let advanced users execute SQL directly against DbConnection
/// without pulling Dapper, EF Core, or any external micro-ORM dependency.
/// </summary>
public static class ForgeDbConnectionExtensions
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IReadOnlyList<T> Query<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
    {
        EnsureOpen(connection);
        return ForgePerformancePipeline.QueryAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async Task<IReadOnlyList<T>> QueryAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        return await ForgePerformancePipeline.QueryAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public static T? QueryFirstOrDefault<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => ForgePerformancePipeline.FirstOrDefaultAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async Task<T?> QueryFirstOrDefaultAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        return await ForgePerformancePipeline.FirstOrDefaultAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public static T QuerySingle<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
    {
        var item = ForgePerformancePipeline.SingleOrDefaultAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds)
            .GetAwaiter()
            .GetResult();
        return item is not null ? item : throw new InvalidOperationException("Sequence contains no elements.");
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async Task<T> QuerySingleAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        var item = await ForgePerformancePipeline.SingleOrDefaultAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
        return item is not null ? item : throw new InvalidOperationException("Sequence contains no elements.");
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public static T? QuerySingleOrDefault<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => ForgePerformancePipeline.SingleOrDefaultAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async Task<T?> QuerySingleOrDefaultAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        return await ForgePerformancePipeline.SingleOrDefaultAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Executes the Execute operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Execute operation.</returns>
    public static int Execute(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
    {
        EnsureOpen(connection);
        return ForgePerformancePipeline.ExecuteAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteAsync operation.</returns>
    public static async Task<int> ExecuteAsync(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        return await ForgePerformancePipeline.ExecuteAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public static T? ExecuteScalar<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
    {
        EnsureOpen(connection);
        return ForgePerformancePipeline.ExecuteScalarAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async Task<T?> ExecuteScalarAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        return await ForgePerformancePipeline.ExecuteScalarAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Executes the QueryMultiple operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the QueryMultiple operation.</returns>
    public static IForgeGridReader QueryMultiple(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, int? timeoutSeconds = null)
    {
        EnsureOpen(connection);
        var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(connection, command, command.ExecuteReader());
    }

    /// <summary>
    /// Executes the QueryMultipleAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the QueryMultipleAsync operation.</returns>
    public static async Task<IForgeGridReader> QueryMultipleAsync(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(connection, command, await command.ExecuteReaderAsync(cancellationToken));
    }

    private static void EnsureOpen(DbConnection connection)
    {
        if (connection.State != ConnectionState.Open) connection.Open();
    }

    private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
    }
}
