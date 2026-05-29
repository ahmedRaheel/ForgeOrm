using System.Data.Common;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core;
using Oracle.ManagedDataAccess.Client;

namespace ForgeORM.Providers.Oracle;

internal static class OracleNativeBulk
{

    public static async ValueTask BulkInsertAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0)
            return;

        options ??= ForgeProviderBulkOptionsDefaults.Current;

        switch (options.OracleStrategy)
        {
            case ForgeBulkOperationStrategy.OracleArrayBinding:
                await InsertWithNativeStrategyAsync(connection, tableName, rows, cancellationToken).ConfigureAwait(false);
                return;

            case ForgeBulkOperationStrategy.OracleMerge:
                await InsertWithTempTableFallbackAsync(connection, tableName, rows, cancellationToken).ConfigureAwait(false);
                return;

            default:
                await InsertWithNativeStrategyAsync(connection, tableName, rows, cancellationToken).ConfigureAwait(false);
                return;
        }
    }

    public static async ValueTask BulkUpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        string keyColumn,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0)
            return;

        options ??= ForgeProviderBulkOptionsDefaults.Current;

        switch (options.OracleStrategy)
        {
            case ForgeBulkOperationStrategy.OracleMerge:
                await UpdateWithTempTableStrategyAsync(connection, tableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
                return;

            case ForgeBulkOperationStrategy.OracleArrayBinding:
                await UpdateWithNativeStrategyAsync(connection, tableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
                return;

            default:
                await UpdateWithTempTableStrategyAsync(connection, tableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
                return;
        }
    }

    public static async ValueTask BulkDeleteAsync<TKey>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<TKey> keys,
        string keyColumn,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (keys is null || keys.Count == 0)
            return;

        options ??= ForgeProviderBulkOptionsDefaults.Current;

        switch (options.OracleStrategy)
        {
            case ForgeBulkOperationStrategy.OracleMerge:
                await DeleteWithTempTableStrategyAsync(connection, tableName, keys, keyColumn, cancellationToken).ConfigureAwait(false);
                return;

            case ForgeBulkOperationStrategy.OracleArrayBinding:
                await DeleteWithNativeStrategyAsync(connection, tableName, keys, keyColumn, cancellationToken).ConfigureAwait(false);
                return;

            default:
                await DeleteWithTempTableStrategyAsync(connection, tableName, keys, keyColumn, cancellationToken).ConfigureAwait(false);
                return;
        }
    }

    private static ValueTask InsertWithNativeStrategyAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken)
        => BulkFallback.InsertAsync(connection, tableName, rows, cancellationToken);

    private static ValueTask InsertWithTempTableFallbackAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken)
        => BulkFallback.InsertAsync(connection, tableName, rows, cancellationToken);

    private static ValueTask UpdateWithNativeStrategyAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken)
        => BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);

    private static ValueTask UpdateWithTempTableStrategyAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken)
        => BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);

    private static async ValueTask DeleteWithNativeStrategyAsync<TKey>(DbConnection connection, string tableName, IReadOnlyCollection<TKey> keys, string keyColumn, CancellationToken cancellationToken)
    { 
        _ = BulkFallback.DeleteAsync(connection, tableName, keys, keyColumn, cancellationToken); 
    }

    private static async ValueTask DeleteWithTempTableStrategyAsync<TKey>(DbConnection connection, string tableName, IReadOnlyCollection<TKey> keys, string keyColumn, CancellationToken cancellationToken)
    { 
       _= BulkFallback.DeleteAsync(connection, tableName, keys, keyColumn, cancellationToken); 
    }

    internal static PropertyInfo[] GetBulkProperties<T>()
        => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsScalar(p.PropertyType))
            .ToArray();

    private static bool IsScalar(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive || actual.IsEnum || actual == typeof(string) || actual == typeof(Guid) || actual == typeof(decimal) || actual == typeof(DateTime) || actual == typeof(DateTimeOffset) || actual == typeof(DateOnly) || actual == typeof(TimeOnly) || actual == typeof(TimeSpan) || actual == typeof(byte[]);
    }
}
