using System.Buffers;
using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

internal static class ForgeSqlServerTvpBatching
{
    public const string DefaultIntListTypeName = "dbo.IntIdList";
    public const string DefaultLongListTypeName = "dbo.BigIntIdList";
    public const string DefaultGuidListTypeName = "dbo.GuidIdList";

    public static SqlParameter CreateIdsParameter<T>(string name, IReadOnlyCollection<T> ids, string? typeName = null)
    {
        var actualType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        var table = new DataTable();
        if (actualType == typeof(int))
        {
            table.Columns.Add("Id", typeof(int));
            foreach (var id in ids) table.Rows.Add(id);
            return new SqlParameter(name, SqlDbType.Structured) { TypeName = typeName ?? DefaultIntListTypeName, Value = table };
        }
        if (actualType == typeof(long))
        {
            table.Columns.Add("Id", typeof(long));
            foreach (var id in ids) table.Rows.Add(id);
            return new SqlParameter(name, SqlDbType.Structured) { TypeName = typeName ?? DefaultLongListTypeName, Value = table };
        }
        if (actualType == typeof(Guid))
        {
            table.Columns.Add("Id", typeof(Guid));
            foreach (var id in ids) table.Rows.Add(id);
            return new SqlParameter(name, SqlDbType.Structured) { TypeName = typeName ?? DefaultGuidListTypeName, Value = table };
        }

        throw new NotSupportedException($"TVP batching currently supports int, long and Guid keys. Type '{actualType.Name}' used expansion fallback.");
    }

    public static string CreateSqlServerTypesScript() => """
IF TYPE_ID(N'dbo.IntIdList') IS NULL CREATE TYPE dbo.IntIdList AS TABLE (Id INT NOT NULL PRIMARY KEY);
IF TYPE_ID(N'dbo.BigIntIdList') IS NULL CREATE TYPE dbo.BigIntIdList AS TABLE (Id BIGINT NOT NULL PRIMARY KEY);
IF TYPE_ID(N'dbo.GuidIdList') IS NULL CREATE TYPE dbo.GuidIdList AS TABLE (Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);
""";
}
