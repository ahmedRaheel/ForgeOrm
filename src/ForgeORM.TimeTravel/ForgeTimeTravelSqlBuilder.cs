using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.TimeTravel;

public sealed class ForgeTimeTravelSqlBuilder : IForgeTimeTravelSqlBuilder
{
    /// <summary>
    /// Executes the BuildSql operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the BuildSql operation.</returns>
    public TimeTravelSql BuildSql(TimeTravelQuery query, string provider = "SqlServer")
    {
        var sql = provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? $"SELECT * FROM {query.Entity} FOR SYSTEM_TIME AS OF @asOf" + (string.IsNullOrWhiteSpace(query.Filter) ? "" : $" WHERE {query.Filter}")
            : $"SELECT * FROM {query.Entity}_history WHERE valid_from <= @asOf AND valid_to > @asOf" + (string.IsNullOrWhiteSpace(query.Filter) ? "" : $" AND {query.Filter}");
        return new TimeTravelSql(sql, new Dictionary<string, object> { ["asOf"] = query.AsOfUtc });
    }
}
