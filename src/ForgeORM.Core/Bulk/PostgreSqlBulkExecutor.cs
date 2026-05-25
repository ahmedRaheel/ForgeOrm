namespace ForgeORM.Core;

internal sealed class PostgreSqlBulkExecutor : ForgeBulkExecutorBase
{
    public static readonly PostgreSqlBulkExecutor Instance = new();
    private PostgreSqlBulkExecutor() { }
    protected override string FormatParameterName(string name) => "@" + name;
}
