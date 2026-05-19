namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Renders Forge report definitions to SQL.
/// </summary>
public static class ForgeReportSqlRenderer
{
    public static string Render(ForgeReportDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Table))
        {
            throw new InvalidOperationException("Report table is required. Call From(table) before rendering.");
        }

        if (definition.Unpivot is not null)
        {
            return RenderUnpivot(definition);
        }

        var selectParts = new List<string>();

        selectParts.AddRange(definition.Dimensions.Select(x => $"{x.Expression} AS [{x.Name}]"));
        selectParts.AddRange(definition.Measures.Select(x => $"{x.Aggregate}({x.Expression}) AS [{x.Alias}]"));
        selectParts.AddRange(definition.Windows.Select(RenderWindow));

        if (definition.Pivot is not null)
        {
            var p = definition.Pivot;
            selectParts.Add($"{p.Aggregate}({p.ValueExpression}) AS [{p.Alias}]");
        }

        if (selectParts.Count == 0)
        {
            selectParts.Add("*");
        }

        var top = definition.Top.HasValue ? $"TOP ({definition.Top.Value}) " : string.Empty;
        var sql = $"SELECT {top}{string.Join(", ", selectParts)}\nFROM {definition.Table}";

        if (!string.IsNullOrWhiteSpace(definition.WhereSql))
        {
            sql += $"\nWHERE {definition.WhereSql}";
        }

        var groupBy = definition.Dimensions.Select(x => x.Expression).ToList();
        if (definition.Pivot is not null)
        {
            groupBy.Add(definition.Pivot.RowExpression);
            groupBy.Add(definition.Pivot.ColumnExpression);
        }

        groupBy = groupBy.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (groupBy.Count > 0 && (definition.Measures.Count > 0 || definition.Pivot is not null))
        {
            sql += $"\nGROUP BY {string.Join(", ", groupBy)}";
        }

        if (!string.IsNullOrWhiteSpace(definition.OrderBySql))
        {
            sql += $"\nORDER BY {definition.OrderBySql}";
        }

        return sql;
    }

    private static string RenderUnpivot(ForgeReportDefinition definition)
    {
        var unpivot = definition.Unpivot!;
        if (unpivot.SourceColumns.Count == 0)
        {
            throw new InvalidOperationException("Unpivot requires at least one source column.");
        }

        var selectColumns = definition.Dimensions.Count == 0
            ? "*"
            : string.Join(", ", definition.Dimensions.Select(x => $"{x.Expression} AS [{x.Name}]"));

        return $"""
SELECT {selectColumns}, [{unpivot.NameColumn}], [{unpivot.ValueColumn}]
FROM {definition.Table}
UNPIVOT
(
    [{unpivot.ValueColumn}] FOR [{unpivot.NameColumn}] IN ({string.Join(", ", unpivot.SourceColumns.Select(x => $"[{x.Trim('[', ']')}]") )})
) AS ForgeUnpivoted
""";
    }

    private static string RenderWindow(ForgeReportWindow window)
    {
        var expression = string.IsNullOrWhiteSpace(window.Expression) ? string.Empty : window.Expression;
        var partition = window.PartitionBy.Count == 0 ? string.Empty : "PARTITION BY " + string.Join(", ", window.PartitionBy);
        var order = window.OrderBy.Count == 0 ? string.Empty : "ORDER BY " + string.Join(", ", window.OrderBy);
        var frame = string.IsNullOrWhiteSpace(window.FrameClause) ? string.Empty : window.FrameClause;
        var overParts = string.Join(" ", new[] { partition, order, frame }.Where(x => !string.IsNullOrWhiteSpace(x)));

        var noArgumentFunction = window.Function.Equals("ROW_NUMBER", StringComparison.OrdinalIgnoreCase)
            || window.Function.Equals("RANK", StringComparison.OrdinalIgnoreCase)
            || window.Function.Equals("DENSE_RANK", StringComparison.OrdinalIgnoreCase)
            || window.Function.Equals("PERCENT_RANK", StringComparison.OrdinalIgnoreCase)
            || window.Function.Equals("CUME_DIST", StringComparison.OrdinalIgnoreCase);

        var functionSql = noArgumentFunction
            ? $"{window.Function}()"
            : string.IsNullOrWhiteSpace(expression)
                ? window.Function
                : $"{window.Function}({expression})";

        return $"{functionSql} OVER ({overParts}) AS [{window.Alias}]";
    }
}
