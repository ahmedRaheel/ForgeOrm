using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

public interface IForgeDb :
    IForgeRawSql,
    IForgeStoredProcedures,
    IForgeDatabaseFunctions,
    IForgeRepository,
    IForgeQueryableFactory,
    IForgeSplitQueryFactory,
    IForgeBulkOperations,
    IForgeBulkConditionOperations,
    IForgeTransactionManager,
    IForgeDiagnostics
{
    IForgeDatabaseProvider Provider { get; }
}
