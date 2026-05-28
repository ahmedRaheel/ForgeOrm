
namespace ForgeORM.Core.Bulk;

public enum ForgeBulkStrategy
{
    SqlDataRecord = 0,
    TableTypeParameter = 1,
    SqlBulkCopy = 2,

    PostgreSqlCopy = 10,
    PostgreSqlTempTable = 11,

    MySqlMultiRow = 20,
    MySqlTempTable = 21,

    OracleArrayBinding = 30,
    OracleMerge = 31
}

public sealed class ForgeBulkOperationOptions
{
    public ForgeBulkStrategy InsertStrategy { get; set; } = ForgeBulkStrategy.SqlDataRecord;
    public ForgeBulkStrategy UpdateStrategy { get; set; } = ForgeBulkStrategy.SqlDataRecord;
    public ForgeBulkStrategy DeleteStrategy { get; set; } = ForgeBulkStrategy.SqlDataRecord;
    public ForgeBulkStrategy GraphUpdateStrategy { get; set; } = ForgeBulkStrategy.SqlDataRecord;

    public bool AutoCreateStructures { get; set; } = true;
    public bool AutoRecreateMismatchedStructures { get; set; } = true;

    public int BatchSize { get; set; } = 5000;
    public int CommandTimeoutSeconds { get; set; } = 0;
}
