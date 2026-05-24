namespace ForgeORM.Core;

internal sealed class SqlServerBulkExecutor : ForgeBulkExecutorBase
{
    public static readonly SqlServerBulkExecutor Instance = new();
    private SqlServerBulkExecutor() { }
    protected override string FormatParameterName(string name) => "@" + name;
}

internal sealed class PostgreSqlBulkExecutor : ForgeBulkExecutorBase
{
    public static readonly PostgreSqlBulkExecutor Instance = new();
    private PostgreSqlBulkExecutor() { }
    protected override string FormatParameterName(string name) => "@" + name;
}

internal sealed class MySqlBulkExecutor : ForgeBulkExecutorBase
{
    public static readonly MySqlBulkExecutor Instance = new();
    private MySqlBulkExecutor() { }
    protected override string FormatParameterName(string name) => "@" + name;
}

internal sealed class OracleBulkExecutor : ForgeBulkExecutorBase
{
    public static readonly OracleBulkExecutor Instance = new();
    private OracleBulkExecutor() { }

    protected override string FormatParameterName(string name) => ":" + name;

    protected override string RewriteSql(string sql)
        => sql.Replace("@", ":", System.StringComparison.Ordinal);
}

internal sealed class GenericBulkExecutor : ForgeBulkExecutorBase
{
    public static readonly GenericBulkExecutor Instance = new();
    private GenericBulkExecutor() { }
}
