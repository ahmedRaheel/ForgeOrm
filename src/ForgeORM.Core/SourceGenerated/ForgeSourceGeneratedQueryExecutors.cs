using System.Threading;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

/// <summary>
/// Source-generated, whole-query SQL Server executor. This is the fastest ForgeORM path:
/// generated code owns command text, parameter binding, execution behavior and materialization.
/// The generic runtime pipeline remains the fallback for dynamic SQL.
/// </summary>
public interface IForgeSqlServerQueryExecutorProvider
{
    bool TryFirstOrDefaultAsync<T>(
        string sql,
        SqlConnection connection,
        object? parameters,
        SqlTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<T?> result);

    bool TryQueryAsync<T>(
        string sql,
        SqlConnection connection,
        object? parameters,
        SqlTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<IReadOnlyList<T>> result);
}

/// <summary>
/// Registry used by ForgeORM.SourceGenerators to register complete query executors with zero runtime shape/binder lookup.
/// </summary>
public static class ForgeSqlServerQueryExecutorRegistry
{
    private static IForgeSqlServerQueryExecutorProvider[] Providers = [];

    public static bool HasProviders => Volatile.Read(ref Providers).Length != 0;

    public static void Register(IForgeSqlServerQueryExecutorProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        while (true)
        {
            var snapshot = Volatile.Read(ref Providers);
            var updated = new IForgeSqlServerQueryExecutorProvider[snapshot.Length + 1];
            Array.Copy(snapshot, updated, snapshot.Length);
            updated[^1] = provider;

            if (ReferenceEquals(Interlocked.CompareExchange(ref Providers, updated, snapshot), snapshot))
                return;
        }
    }

    public static bool TryFirstOrDefaultAsync<T>(
        string sql,
        SqlConnection connection,
        object? parameters,
        SqlTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<T?> result)
    {
        var providers = Volatile.Read(ref Providers);
        for (var i = 0; i < providers.Length; i++)
        {
            if (providers[i].TryFirstOrDefaultAsync(sql, connection, parameters, transaction, timeoutSeconds, cancellationToken, out result))
                return true;
        }

        result = default;
        return false;
    }

    public static bool TryQueryAsync<T>(
        string sql,
        SqlConnection connection,
        object? parameters,
        SqlTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<IReadOnlyList<T>> result)
    {
        var providers = Volatile.Read(ref Providers);
        for (var i = 0; i < providers.Length; i++)
        {
            if (providers[i].TryQueryAsync(sql, connection, parameters, transaction, timeoutSeconds, cancellationToken, out result))
                return true;
        }

        result = default;
        return false;
    }
}
