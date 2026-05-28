
using ForgeORM.Core;
using ForgeORM.Core.Bulk;

namespace ForgeORM.Providers.SqlServer;

internal static class SqlServerBulkPlanCache<T>
{
    public static ForgeBulkPlan GetInsertPlan(string tableName)
        => new()
        {
            TableName = tableName,
            QuotedTableName = tableName,
            InsertSql = string.Empty,
            UpdateSql = string.Empty,
            DeleteSql = string.Empty,
            KeyColumn = "Id"
        };

    public static ForgeBulkPlan GetUpdatePlan(string tableName, string keyColumn)
        => new()
        {
            TableName = tableName,
            QuotedTableName = tableName,
            InsertSql = string.Empty,
            UpdateSql = string.Empty,
            DeleteSql = string.Empty,
            KeyColumn = keyColumn
        };

    public static ForgeBulkPlan GetDeletePlan(string tableName, string keyColumn)
        => new()
        {
            TableName = tableName,
            QuotedTableName = tableName,
            InsertSql = string.Empty,
            UpdateSql = string.Empty,
            DeleteSql = string.Empty,
            KeyColumn = keyColumn
        };
}
