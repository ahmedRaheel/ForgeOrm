using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public interface IForgeAiMigrationPlanner
/// <summary>
/// Defines the PlanAddColumn operation.
/// </summary>
/// <param name="table">The table value.</param>
/// <param name="column">The column value.</param>
/// <param name="sqlType">The sqlType value.</param>
/// <param name="nullable">The nullable value.</param>
/// <returns>The result of the PlanAddColumn operation.</returns>
{
    /// <summary>
    /// Defines the PlanAddColumn operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="column">The column value.</param>
    /// <param name="sqlType">The sqlType value.</param>
    /// <param name="nullable">The nullable value.</param>
    /// <returns>The result of the PlanAddColumn operation.</returns>
    ForgeMigrationPlan PlanAddColumn(string table, string column, string sqlType, bool nullable = true);
}
