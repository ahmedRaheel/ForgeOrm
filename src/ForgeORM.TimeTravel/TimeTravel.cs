using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.TimeTravel;

public sealed record TimeTravelQuery(string Entity, DateTimeOffset AsOfUtc, string? Filter = null);
public sealed record TimeTravelSql(string Sql, IReadOnlyDictionary<string,object> Parameters);

public interface IForgeTimeTravelSqlBuilder
{
    TimeTravelSql BuildSql(TimeTravelQuery query, string provider = "SqlServer");
}

public sealed class ForgeTimeTravelSqlBuilder : IForgeTimeTravelSqlBuilder
{
    /// <summary>
    /// Initializes or executes the BuildSql operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="provider">The provider value.</param>
    /// <returns>The operation result.</returns>
    public TimeTravelSql BuildSql(TimeTravelQuery query, string provider = "SqlServer")
    {
        var sql = provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? $"SELECT * FROM {query.Entity} FOR SYSTEM_TIME AS OF @asOf" + (string.IsNullOrWhiteSpace(query.Filter) ? "" : $" WHERE {query.Filter}")
            : $"SELECT * FROM {query.Entity}_history WHERE valid_from <= @asOf AND valid_to > @asOf" + (string.IsNullOrWhiteSpace(query.Filter) ? "" : $" AND {query.Filter}");
        return new TimeTravelSql(sql, new Dictionary<string, object> { ["asOf"] = query.AsOfUtc });
    }
}

public static class ForgeTimeTravelServiceCollectionExtensions
{
    /// <summary>
    /// Initializes or executes the AddForgeTimeTravel operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddForgeTimeTravel(this IServiceCollection services) => services.AddSingleton<IForgeTimeTravelSqlBuilder, ForgeTimeTravelSqlBuilder>();
}
