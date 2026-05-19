namespace ForgeORM.Core.Graph;

/// <summary>
/// Internal bulk execution strategies.
/// </summary>
public enum ForgeBulkStrategy
{
    Auto,
    RowByRow,
    MultiRowInsert,
    SqlBulkCopy,
    TableValuedParameter,
    OpenJson,
    PostgreSqlCopy,
    PostgreSqlJsonRecordset,
    PostgreSqlUnnest,
    MySqlTempTable,
    OracleArrayBinding,
    OracleMerge,
    TempTableMerge
}