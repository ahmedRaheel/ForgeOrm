namespace ForgeORM.Core;

internal sealed class OracleBulkExecutor : ForgeBulkExecutorBase
{
    public static readonly OracleBulkExecutor Instance = new();
    private OracleBulkExecutor() { }

    protected override string FormatParameterName(string name) => ":" + name;

    protected override string RewriteSql(string sql)
        => sql.Replace("@", ":", System.StringComparison.Ordinal);
}
