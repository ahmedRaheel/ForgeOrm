using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryAst;

internal sealed class ForgeAstScriptBuilder : IForgeAstScriptBuilder
{
    private readonly List<ForgeCte> _ctes = [];
    private readonly List<ForgeTempTable> _tempTables = [];
    private readonly List<string> _statements = [];

    public IForgeAstScriptBuilder WithCte(string name, string sql)
    {
        _ctes.Add(new ForgeCte(name, sql));
        return this;
    }

    public IForgeAstScriptBuilder CreateTempTable(string name, Action<IForgeAstTempTableBuilder> configure)
    {
        var builder = new ForgeAstTempTableBuilder(name);
        configure(builder);
        _tempTables.Add(builder.Build());
        return this;
    }

    public IForgeAstScriptBuilder InsertIntoTemp(string tempTable, string selectSql)
    {
        _statements.Add($"INSERT INTO {tempTable} {selectSql}");
        return this;
    }

    public IForgeAstScriptBuilder Statement(string sql)
    {
        _statements.Add(sql);
        return this;
    }

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

internal sealed class ForgeAstTempTableBuilder : IForgeAstTempTableBuilder
{
    private readonly ForgeTempTable _table;

    public ForgeAstTempTableBuilder(string name)
    {
        _table = new ForgeTempTable { Name = name };
    }

    public IForgeAstTempTableBuilder Column(string name, string dbType, bool nullable = true)
    {
        _table.Columns.Add(new ForgeTempColumn(name, dbType, nullable));
        return this;
    }

    public IForgeAstTempTableBuilder PrimaryKey(params string[] columns)
    {
        _table.PrimaryKeyColumns.AddRange(columns);
        return this;
    }

    public ForgeTempTable Build() => _table;
}
