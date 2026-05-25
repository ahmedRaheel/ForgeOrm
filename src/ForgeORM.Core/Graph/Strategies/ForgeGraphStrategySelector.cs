namespace ForgeORM.Core.Graph.Strategies;

/// <summary>
/// Default strategy selector.
/// </summary>
public sealed class ForgeGraphStrategySelector : IForgeGraphStrategySelector
{
    public ForgeBulkStrategy Select(
        ForgeDatabaseProvider provider,
        ForgeGraphOperation operation,
        int rowCount,
        ForgeGraphOptions options)
    {
        if (options.Strategy != ForgeBulkStrategy.Auto)
        {
            return options.Strategy;
        }

        if (!options.UseBulkWhenPossible || rowCount <= 1)
        {
            return ForgeBulkStrategy.RowByRow;
        }

        return provider switch
        {
            ForgeDatabaseProvider.SqlServer => SelectSqlServer(rowCount),
            ForgeDatabaseProvider.PostgreSql => SelectPostgreSql(rowCount),
            ForgeDatabaseProvider.MySql => SelectMySql(rowCount),
            ForgeDatabaseProvider.Oracle => SelectOracle(rowCount),
            _ => ForgeBulkStrategy.MultiRowInsert
        };
    }

    private static ForgeBulkStrategy SelectSqlServer(int rowCount)
    {
        if (rowCount <= 100)
        {
            return ForgeBulkStrategy.OpenJson;
        }

        if (rowCount <= 5000)
        {
            return ForgeBulkStrategy.TableValuedParameter;
        }

        return ForgeBulkStrategy.SqlBulkCopy;
    }

    private static ForgeBulkStrategy SelectPostgreSql(int rowCount)
    {
        if (rowCount <= 100)
        {
            return ForgeBulkStrategy.PostgreSqlJsonRecordset;
        }

        if (rowCount <= 5000)
        {
            return ForgeBulkStrategy.PostgreSqlUnnest;
        }

        return ForgeBulkStrategy.PostgreSqlCopy;
    }

    private static ForgeBulkStrategy SelectMySql(int rowCount)
    {
        if (rowCount <= 500)
        {
            return ForgeBulkStrategy.MultiRowInsert;
        }

        return ForgeBulkStrategy.MySqlTempTable;
    }

    private static ForgeBulkStrategy SelectOracle(int rowCount)
    {
        if (rowCount <= 5000)
        {
            return ForgeBulkStrategy.OracleArrayBinding;
        }

        return ForgeBulkStrategy.OracleMerge;
    }
}