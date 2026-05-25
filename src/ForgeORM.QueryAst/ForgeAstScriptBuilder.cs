using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryAst;

internal sealed class ForgeAstScriptBuilder : IForgeAstScriptBuilder
{
    private readonly List<ForgeCte> _ctes = [];
    private readonly List<ForgeTempTable> _tempTables = [];
    private readonly List<string> _statements = [];

    /// <summary>
    /// Executes the WithCte operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the WithCte operation.</returns>
    public IForgeAstScriptBuilder WithCte(string name, string sql)
    {
        _ctes.Add(new ForgeCte(name, sql));
        return this;
    }

    /// <summary>
    /// Executes the CreateTempTable operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="configure">The configure value.</param>
    /// <returns>The result of the CreateTempTable operation.</returns>
    public IForgeAstScriptBuilder CreateTempTable(string name, Action<IForgeAstTempTableBuilder> configure)
    {
        var builder = new ForgeAstTempTableBuilder(name);
        configure(builder);
        _tempTables.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Executes the InsertIntoTemp operation.
    /// </summary>
    /// <param name="tempTable">The tempTable value.</param>
    /// <param name="selectSql">The selectSql value.</param>
    /// <returns>The result of the InsertIntoTemp operation.</returns>
    public IForgeAstScriptBuilder InsertIntoTemp(string tempTable, string selectSql)
    {
        _statements.Add($"INSERT INTO {tempTable} {selectSql}");
        return this;
    }

    /// <summary>
    /// Executes the Statement operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the Statement operation.</returns>
    public IForgeAstScriptBuilder Statement(string sql)
    {
        _statements.Add(sql);
        return this;
    }

    /// <summary>
    /// Executes the Render operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Render operation.</returns>
    public ForgeRenderedSql Render(IForgeDatabaseProvider provider)
    {
        var sql = new StringBuilder();

        foreach (var temp in _tempTables)
        {
            sql.AppendLine(RenderTempTable(provider, temp));
            sql.AppendLine();
        }

        if (_ctes.Count > 0)
        {
            sql.Append("WITH ");
            sql.AppendLine(string.Join(", ", _ctes.Select(x => $"{x.Name} AS ({x.Sql})")));
        }

        foreach (var statement in _statements)
        {
            sql.AppendLine(statement.TrimEnd(';') + ";");
        }

        return new ForgeRenderedSql(sql.ToString());
    }

    private static string RenderTempTable(IForgeDatabaseProvider provider, ForgeTempTable table)
    {
        var tableName = provider.ProviderName.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)
            ? table.Name.TrimStart('#')
            : table.Name;

        var columns = table.Columns
            .Select(c => $"{c.Name} {c.DbType} {(c.Nullable ? "NULL" : "NOT NULL")}")
            .ToList();

        if (table.PrimaryKeyColumns.Count > 0)
            columns.Add($"PRIMARY KEY ({string.Join(", ", table.PrimaryKeyColumns)})");

        if (provider.ProviderName.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
            return $"CREATE TEMP TABLE {tableName} ({string.Join(", ", columns)});";

        if (provider.ProviderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            return $"CREATE GLOBAL TEMPORARY TABLE {tableName.TrimStart('#')} ({string.Join(", ", columns)}) ON COMMIT PRESERVE ROWS;";

        return $"CREATE TABLE {tableName} ({string.Join(", ", columns)});";
    }
}
