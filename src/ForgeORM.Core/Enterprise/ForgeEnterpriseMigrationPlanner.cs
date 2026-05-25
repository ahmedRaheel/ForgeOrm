using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Enterprise migration planner foundation.
/// </summary>
public static class ForgeEnterpriseMigrationPlanner
{
    public static IReadOnlyList<ForgeMigrationOperation> PlanAddColumn(
        string table,
        string column,
        string sqlType,
        bool nullable)
    {
        var nullSql = nullable ? "NULL" : "NOT NULL";
        return
        [
            new("AddColumn", $"{table}.{column}", $"ALTER TABLE {table} ADD {column} {sqlType} {nullSql};")
        ];
    }
}
