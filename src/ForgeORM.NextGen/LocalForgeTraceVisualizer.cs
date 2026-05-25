
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ForgeORM.NextGen;

public sealed class LocalForgeTraceVisualizer : IForgeTraceVisualizer
{
    /// <summary>
    /// Executes the CreateTrace operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="providerName">The providerName value.</param>
    /// <returns>The result of the CreateTrace operation.</returns>
    public ForgeTraceLink CreateTrace(string sql, object? parameters, string providerName)
    {
        var id = Guid.NewGuid().ToString("N");
        return new ForgeTraceLink
        {
            TraceId = id,
            LocalUrl = "http://localhost:5055/forge-trace/" + id,
            Sql = sql,
            Parameters = parameters,
            ProviderName = providerName,
            HotPaths = sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase)
                ? ["SELECT * may cause unnecessary IO and mapping overhead."]
                : []
        };
    }
}
