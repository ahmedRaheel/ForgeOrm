using System.Data;
using System.Data.Common;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// Dapper-style extension methods owned by ForgeORM.
/// These APIs let advanced users execute SQL directly against DbConnection
/// without pulling Dapper, EF Core, or any external micro-ORM dependency.
/// </summary>
public static class ForgeDbConnectionExtensions
{
    /// <summary>
    /// Initializes or executes the Query operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public static IReadOnlyList<T> Query<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
    {
        EnsureOpen(connection);
        return ForgeAdo.Query<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds);
    }

    /// <summary>
    /// Initializes or executes the QueryAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public static async Task<IReadOnlyList<T>> QueryAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        return await ForgeAdo.QueryAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Initializes or executes the QueryFirstOrDefault operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public static T? QueryFirstOrDefault<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => connection.Query<T>(sql, parameters, transaction, commandType, timeoutSeconds).FirstOrDefault();

    /// <summary>
    /// Initializes or executes the QueryFirstOrDefaultAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public static async Task<T?> QueryFirstOrDefaultAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await connection.QueryAsync<T>(sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken)).FirstOrDefault();

    /// <summary>
    /// Initializes or executes the QuerySingle operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public static T QuerySingle<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => connection.Query<T>(sql, parameters, transaction, commandType, timeoutSeconds).Single();

    /// <summary>
    /// Initializes or executes the QuerySingleAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public static async Task<T> QuerySingleAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await connection.QueryAsync<T>(sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken)).Single();

    /// <summary>
    /// Initializes or executes the QuerySingleOrDefault operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public static T? QuerySingleOrDefault<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => connection.Query<T>(sql, parameters, transaction, commandType, timeoutSeconds).SingleOrDefault();

    /// <summary>
    /// Initializes or executes the QuerySingleOrDefaultAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public static async Task<T?> QuerySingleOrDefaultAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await connection.QueryAsync<T>(sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken)).SingleOrDefault();

    /// <summary>
    /// Initializes or executes the Execute operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public static int Execute(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
    {
        EnsureOpen(connection);
        return ForgeAdo.Execute(connection, sql, parameters, transaction, commandType, timeoutSeconds);
    }

    /// <summary>
    /// Initializes or executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public static async Task<int> ExecuteAsync(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        return await ForgeAdo.ExecuteAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Initializes or executes the ExecuteScalar operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public static T? ExecuteScalar<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
    {
        EnsureOpen(connection);
        return ForgeAdo.ExecuteScalar<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds);
    }

    /// <summary>
    /// Initializes or executes the ExecuteScalarAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public static async Task<T?> ExecuteScalarAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        return await ForgeAdo.ExecuteScalarAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Initializes or executes the QueryMultiple operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public static IForgeGridReader QueryMultiple(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, int? timeoutSeconds = null)
    {
        EnsureOpen(connection);
        var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(connection, command, command.ExecuteReader());
    }

    /// <summary>
    /// Initializes or executes the QueryMultipleAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
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
