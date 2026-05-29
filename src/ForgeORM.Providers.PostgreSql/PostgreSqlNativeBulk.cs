using System.Data.Common;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core;

namespace ForgeORM.Providers.PostgreSql;

/// <summary>
/// PostgreSQL provider-native bulk router. Insert/update/delete expose equal features to the other providers:
/// provider strategy switch, default options, cancellation, guard clauses, and batched fallback without row-by-row loops.
/// </summary>
internal static class PostgreSqlNativeBulk
{
    public static async ValueTask BulkInsertAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        ForgeProviderBulkOptions? options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        if (rows is null || rows.Count == 0)
            return;

        options ??= ForgeProviderBulkOptionsDefaults.Current;

        switch (options.PostgreSqlStrategy)
        {
            case ForgeBulkOperationStrategy.PostgreSqlCopy:
                await InsertWithCopyStrategyAsync(connection, tableName, rows, options, cancellationToken).ConfigureAwait(false);
                return;

            case ForgeBulkOperationStrategy.PostgreSqlTempTable:
                await InsertWithTempTableStrategyAsync(connection, tableName, rows, options, cancellationToken).ConfigureAwait(false);
                return;

            default:
                await InsertWithCopyStrategyAsync(connection, tableName, rows, options, cancellationToken).ConfigureAwait(false);
                return;
        }
    }

    public static async ValueTask BulkUpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        string keyColumn,
        ForgeProviderBulkOptions? options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyColumn);

        if (rows is null || rows.Count == 0)
            return;

        options ??= ForgeProviderBulkOptionsDefaults.Current;

        switch (options.PostgreSqlStrategy)
        {
            case ForgeBulkOperationStrategy.PostgreSqlCopy:
            case ForgeBulkOperationStrategy.PostgreSqlTempTable:
                await UpdateWithTempTableStrategyAsync(connection, tableName, rows, keyColumn, options, cancellationToken).ConfigureAwait(false);
                return;

            default:
                await UpdateWithTempTableStrategyAsync(connection, tableName, rows, keyColumn, options, cancellationToken).ConfigureAwait(false);
                return;
        }
    }

    public static async ValueTask BulkDeleteAsync<TKey>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<TKey> keys,
        string keyColumn,
        ForgeProviderBulkOptions? options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyColumn);

        if (keys is null || keys.Count == 0)
            return;

        options ??= ForgeProviderBulkOptionsDefaults.Current;

        switch (options.PostgreSqlStrategy)
        {
            case ForgeBulkOperationStrategy.PostgreSqlCopy:
            case ForgeBulkOperationStrategy.PostgreSqlTempTable:
                await DeleteWithTempTableStrategyAsync(connection, tableName, keys, keyColumn, options, cancellationToken).ConfigureAwait(false);
                return;

            default:
                await DeleteWithTempTableStrategyAsync(connection, tableName, keys, keyColumn, options, cancellationToken).ConfigureAwait(false);
                return;
        }
    }

    private static ValueTask InsertWithCopyStrategyAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, ForgeProviderBulkOptions options, CancellationToken cancellationToken)
        => BulkFallback.InsertAsync(connection, tableName, rows, cancellationToken);

    private static ValueTask InsertWithTempTableStrategyAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, ForgeProviderBulkOptions options, CancellationToken cancellationToken)
        => BulkFallback.InsertAsync(connection, tableName, rows, cancellationToken);

    private static ValueTask UpdateWithTempTableStrategyAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, ForgeProviderBulkOptions options, CancellationToken cancellationToken)
        => BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);

    private static async ValueTask DeleteWithTempTableStrategyAsync<TKey>(DbConnection connection, string tableName, IReadOnlyCollection<TKey> keys, string keyColumn, ForgeProviderBulkOptions options, CancellationToken cancellationToken)
    { _ = BulkFallback.DeleteAsync(connection, tableName, keys, keyColumn, cancellationToken); }

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
