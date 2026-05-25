using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Compiles a SQL Server single-key query into the lowest-allocation ForgeORM execution path.
    /// The returned query caches SQL, parameter name, typed SQL parameter binder, command behavior,
    /// and the SQL Server direct materializer after the first execution.
    /// </summary>
    public ForgeSqlServerCompiledQuery<T, TKey> CompileQuery<T, TKey>(string sql, int? timeoutSeconds = null)
    {
        if (!IsSqlServerProvider())
            throw new NotSupportedException("CompileQuery<T,TKey> currently uses the SQL Server provider-direct fast lane. Use QuerySingle/QuerySingleAsync for provider-neutral execution.");

        return new ForgeSqlServerCompiledQuery<T, TKey>(_connectionString, sql, timeoutSeconds);
    }
}
