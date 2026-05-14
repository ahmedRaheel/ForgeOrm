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
    public static IReadOnlyList<T> Query<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
    {
        EnsureOpen(connection);
        return ForgeAdo.Query<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds);
    }

    public static async Task<IReadOnlyList<T>> QueryAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        return await ForgeAdo.QueryAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    public static T? QueryFirstOrDefault<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => connection.Query<T>(sql, parameters, transaction, commandType, timeoutSeconds).FirstOrDefault();

    public static async Task<T?> QueryFirstOrDefaultAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await connection.QueryAsync<T>(sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken)).FirstOrDefault();

    public static T QuerySingle<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => connection.Query<T>(sql, parameters, transaction, commandType, timeoutSeconds).Single();

    public static async Task<T> QuerySingleAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await connection.QueryAsync<T>(sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken)).Single();

    public static T? QuerySingleOrDefault<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => connection.Query<T>(sql, parameters, transaction, commandType, timeoutSeconds).SingleOrDefault();

    public static async Task<T?> QuerySingleOrDefaultAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await connection.QueryAsync<T>(sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken)).SingleOrDefault();

    public static int Execute(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
    {
        EnsureOpen(connection);
        return ForgeAdo.Execute(connection, sql, parameters, transaction, commandType, timeoutSeconds);
    }

    public static async Task<int> ExecuteAsync(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        return await ForgeAdo.ExecuteAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    public static T? ExecuteScalar<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
    {
        EnsureOpen(connection);
        return ForgeAdo.ExecuteScalar<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds);
    }

    public static async Task<T?> ExecuteScalarAsync<T>(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);
        return await ForgeAdo.ExecuteScalarAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
    }

    public static IForgeGridReader QueryMultiple(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, int? timeoutSeconds = null)
    {
        EnsureOpen(connection);
        var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(connection, command, command.ExecuteReader());
    }

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
