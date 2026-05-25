using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

/// <summary>
/// Registry used by ForgeORM.SourceGenerators to register complete query executors with zero runtime shape/binder lookup.
/// </summary>
public static class ForgeSqlServerQueryExecutorRegistry
{
    private static readonly List<IForgeSqlServerQueryExecutorProvider> Providers = new();
    private static readonly object Gate = new();

    public static bool HasProviders
    {
        get { lock (Gate) return Providers.Count != 0; }
    }

    public static void Register(IForgeSqlServerQueryExecutorProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (Gate) Providers.Add(provider);
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
        lock (Gate)
        {
            for (var i = 0; i < Providers.Count; i++)
            {
                if (Providers[i].TryFirstOrDefaultAsync(sql, connection, parameters, transaction, timeoutSeconds, cancellationToken, out result))
                    return true;
            }
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
        lock (Gate)
        {
            for (var i = 0; i < Providers.Count; i++)
            {
                if (Providers[i].TryQueryAsync(sql, connection, parameters, transaction, timeoutSeconds, cancellationToken, out result))
                    return true;
            }
        }

        result = default;
        return false;
    }
}
