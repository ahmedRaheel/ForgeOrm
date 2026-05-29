namespace ForgeORM.Abstractions;

/// <summary>
/// Provider-native bulk execution strategy.
/// Default for SQL Server insert is SqlBulkCopy; TVP strategies remain available for update/delete and graph paths.
/// Other providers map to their native equivalent.
/// </summary>
public enum ForgeBulkOperationStrategy
{
    /// <summary>
    /// SQL Server TVP path: SqlDataRecord + TVP.
    /// Insert: INSERT SELECT FROM @Rows.
    /// Update: MERGE FROM @Rows.
    /// Delete: DELETE JOIN FROM key TVP.
    /// </summary>
    SqlDataRecord = 0,

    /// <summary>
    /// SQL Server fallback path: DataTable table-valued parameter.
    /// </summary>
    TableTypeParameter = 1,

    /// <summary>
    /// SQL Server fastest path for append-only insert. Update/delete use staging table semantics when this option is selected.
    /// </summary>
    SqlBulkCopy = 2,

    /// <summary>
    /// PostgreSQL primary path: COPY into staging/target.
    /// </summary>
    PostgreSqlCopy = 10,

    /// <summary>
    /// PostgreSQL fallback/update/delete path: temp table + UPDATE FROM / DELETE USING.
    /// </summary>
    PostgreSqlTempTable = 11,

    /// <summary>
    /// MySQL primary insert path: batched multi-row insert.
    /// </summary>
    MySqlMultiRowInsert = 20,

    /// <summary>
    /// MySQL update/delete path: temporary table + UPDATE JOIN / DELETE JOIN.
    /// </summary>
    MySqlTempTable = 21,

    /// <summary>
    /// Oracle primary path: Array Binding.
    /// </summary>
    OracleArrayBinding = 30,

    /// <summary>
    /// Oracle update/graph path: MERGE using bind arrays or staging.
    /// </summary>
    OracleMerge = 31
}

