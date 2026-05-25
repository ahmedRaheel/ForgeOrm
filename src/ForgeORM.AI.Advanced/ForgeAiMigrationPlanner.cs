using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public sealed class ForgeAiMigrationPlanner : IForgeAiMigrationPlanner
{
    /// <summary>
    /// Executes the PlanAddColumn operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="column">The column value.</param>
    /// <param name="sqlType">The sqlType value.</param>
    /// <param name="nullable">The nullable value.</param>
    /// <returns>The result of the PlanAddColumn operation.</returns>
    public ForgeMigrationPlan PlanAddColumn(string table, string column, string sqlType, bool nullable = true)
    {
        var nullability = nullable ? "NULL" : "NOT NULL";
        return new ForgeMigrationPlan(
            $"Add_{table}_{column}",
            [$"ALTER TABLE {table} ADD {column} {sqlType} {nullability};"],
            [$"ALTER TABLE {table} DROP COLUMN {column};"],
            nullable ? [] : [$"Column {column} is NOT NULL. Add DEFAULT or backfill strategy for existing rows."]);
    }
}
