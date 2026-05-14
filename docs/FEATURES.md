# ForgeORM Complete Feature List

## Raw SQL-native
Query, QueryAsync, QueryFirst, QueryFirstAsync, QueryFirstOrDefault, QueryFirstOrDefaultAsync, QuerySingle, QuerySingleAsync, QuerySingleOrDefault, QuerySingleOrDefaultAsync, Execute, ExecuteAsync, ExecuteScalar, ExecuteScalarAsync, QueryMultiple, QueryMultipleAsync.

## Stored Procedures
QueryProcedure, QueryProcedureAsync, ExecuteProcedure, ExecuteProcedureAsync, ExecuteProcedureScalar, ExecuteProcedureScalarAsync, QueryProcedureMultiple.

## Functions
ExecuteFunction, ExecuteFunctionAsync, QueryFunction, QueryFunctionAsync.

## EF-like
Set<T>(), From<T>(), Sql<T>(), Where, OrderBy, Skip, Take, Count, FirstOrDefault.

## Dynamic Query Builder
Select, From, Where, Join, LeftJoin, GroupBy, Having, OrderBy, Skip, Take.

## Mapping
Reflection object mapping with extension path for compiled/source-generated mapping.

## Split Query
Parent/child graph loading with IncludeMany.

## Bulk
BulkInsert, BulkUpdate, BulkDelete, BulkMerge. Provider-specific hooks are included.

## Transactions
BeginTransaction, Commit, Rollback, sync/async.

## Intelligence
Query suggestions, correction, autocomplete items for keywords, tables, columns.

## Providers
SQL Server / SQL Express, PostgreSQL, MySQL, Oracle, SQLite.
