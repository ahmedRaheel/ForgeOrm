using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.TimeTravel;

public interface IForgeTimeTravelSqlBuilder
/// <summary>
/// Defines the BuildSql operation.
/// </summary>
/// <param name="query">The query value.</param>
/// <param name="provider">The provider value.</param>
/// <returns>The result of the BuildSql operation.</returns>
{
    /// <summary>
    /// Defines the BuildSql operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the BuildSql operation.</returns>
    TimeTravelSql BuildSql(TimeTravelQuery query, string provider = "SqlServer");
}
