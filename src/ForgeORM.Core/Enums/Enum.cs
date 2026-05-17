namespace ForgeORM.Core.Enums
{
    /// <summary>
    /// ForgeDatabaseProvider
    /// </summary>
    public enum ForgeDatabaseProvider
    {
        SqlServer,
        PostgreSql,
        MySql,
        Oracle,
        Sqlite
    }
    /// <summary>
    /// ForgeGraphOperation
    /// </summary>
    public enum ForgeGraphOperation
    {
        Insert,
        Update,
        Delete
    }
    /// <summary>
    /// ForgeBulkStrategy
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

    /// <summary>
    /// ForgeChildSyncMode
    /// </summary>
    public enum ForgeChildSyncMode
    {
        InsertOnly,
        InsertUpdate,
        InsertUpdateDeleteMissing
    }

    /// <summary>
    /// ForgeDeleteMode
    /// </summary>
    public enum ForgeDeleteMode
    {
        HardDelete,
        SoftDelete
    }
}
