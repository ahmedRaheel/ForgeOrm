namespace ForgeORM.Abstractions;

/// <summary>
/// Contract for every executable ForgeORM query shape so common features can be applied centrally.
/// </summary>
public interface IForgeExecutableQuery
{
    ForgeQueryExecutionOptions ExecutionOptions { get; }
    string ToSql();
}
