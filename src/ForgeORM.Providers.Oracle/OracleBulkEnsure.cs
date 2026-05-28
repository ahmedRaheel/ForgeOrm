using Oracle.ManagedDataAccess.Client;

namespace ForgeORM.Providers.Oracle;

internal static class OracleBulkEnsure
{
    public static ValueTask EnsureArrayBindingReadyAsync(
        OracleConnection connection,
        OracleBulkPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (plan.Columns.Count == 0)
            throw new InvalidOperationException("Oracle bulk plan has no columns.");

        return ValueTask.CompletedTask;
    }
}
