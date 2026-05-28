using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using ForgeORM.Core.Bulk;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace ForgeORM.Providers.SqlServer;

internal static class SqlServerNativeBulk
{
    private static readonly ConcurrentDictionary<(Type Type, string Table, string Key), SqlServerBulkPlan> PlanCache = new();

    public static async ValueTask<int> BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0) return 0;
        if (connection is not SqlConnection sql) throw new InvalidOperationException("SQL Server bulk requires SqlConnection.");
        var list = rows as IReadOnlyList<T> ?? rows.ToArray();
        var options = ForgeBulkOperationDefaults.Current;
        var plan = GetPlan<T>(tableName, "Id", includeKeyForColumns: false);
        if (options.AutoCreateStructures) await SqlServerBulkEnsure.EnsureTableTypeCompatibleAsync(sql, plan, SqlServerTableTypePurpose.InsertOrUpdate, cancellationToken).ConfigureAwait(false);
        return options.InsertStrategy switch
        {
            ForgeBulkStrategy.TableTypeParameter => await InsertDataTableAsync(sql, plan, list, cancellationToken).ConfigureAwait(false),
            ForgeBulkStrategy.SqlBulkCopy => await InsertSqlBulkCopyAsync(sql, plan, list, options, cancellationToken).ConfigureAwait(false),
            _ => await InsertSqlDataRecordPrimaryAsync(sql, plan, list, cancellationToken).ConfigureAwait(false)
        };
    }

    public static async ValueTask<int> BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0) return 0;
        if (connection is not SqlConnection sql) throw new InvalidOperationException("SQL Server bulk requires SqlConnection.");
        var list = rows as IReadOnlyList<T> ?? rows.ToArray();
        var options = ForgeBulkOperationDefaults.Current;
        var plan = GetPlan<T>(tableName, keyColumn, includeKeyForColumns: true);
        if (options.AutoCreateStructures) await SqlServerBulkEnsure.EnsureTableTypeCompatibleAsync(sql, plan, SqlServerTableTypePurpose.InsertOrUpdate, cancellationToken).ConfigureAwait(false);
        return options.UpdateStrategy == ForgeBulkStrategy.TableTypeParameter
            ? await UpdateDataTableAsync(sql, plan, list, cancellationToken).ConfigureAwait(false)
            : await UpdateSqlDataRecordPrimaryAsync(sql, plan, list, cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<int> BulkDeleteAsync<TKey>(DbConnection connection, string tableName, IReadOnlyCollection<TKey> keys, string keyColumn, CancellationToken cancellationToken = default)
    {
        if (keys is null || keys.Count == 0) return 0;
        if (connection is not SqlConnection sql) throw new InvalidOperationException("SQL Server bulk requires SqlConnection.");
        var list = keys as IReadOnlyList<TKey> ?? keys.ToArray();
        var options = ForgeBulkOperationDefaults.Current;
        var plan = GetDeletePlan<TKey>(tableName, keyColumn);
        if (options.AutoCreateStructures) await SqlServerBulkEnsure.EnsureTableTypeCompatibleAsync(sql, plan, SqlServerTableTypePurpose.DeleteKeyOnly, cancellationToken).ConfigureAwait(false);
        return options.DeleteStrategy == ForgeBulkStrategy.TableTypeParameter
            ? await DeleteDataTableAsync(sql, plan, list, cancellationToken).ConfigureAwait(false)
            : await DeleteSqlDataRecordPrimaryAsync(sql, plan, list, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<int> InsertSqlDataRecordPrimaryAsync<T>(SqlConnection c, SqlServerBulkPlan p, IReadOnlyList<T> rows, CancellationToken ct)
    { try { return await InsertSqlDataRecordAsync(c,p,rows,ct).ConfigureAwait(false); } catch(Exception ex) when(SqlServerBulkFallbackPolicy.CanFallback(ex)) { return await InsertDataTableAsync(c,p,rows,ct).ConfigureAwait(false); } }
    private static async ValueTask<int> UpdateSqlDataRecordPrimaryAsync<T>(SqlConnection c, SqlServerBulkPlan p, IReadOnlyList<T> rows, CancellationToken ct)
    { try { return await UpdateSqlDataRecordAsync(c,p,rows,ct).ConfigureAwait(false); } catch(Exception ex) when(SqlServerBulkFallbackPolicy.CanFallback(ex)) { return await UpdateDataTableAsync(c,p,rows,ct).ConfigureAwait(false); } }
    private static async ValueTask<int> DeleteSqlDataRecordPrimaryAsync<TKey>(SqlConnection c, SqlServerBulkPlan p, IReadOnlyList<TKey> keys, CancellationToken ct)
    { try { return await DeleteSqlDataRecordAsync(c,p,keys,ct).ConfigureAwait(false); } catch(Exception ex) when(SqlServerBulkFallbackPolicy.CanFallback(ex)) { return await DeleteDataTableAsync(c,p,keys,ct).ConfigureAwait(false); } }

    private static async ValueTask<int> InsertSqlDataRecordAsync<T>(SqlConnection c, SqlServerBulkPlan p, IReadOnlyList<T> rows, CancellationToken ct)
    { await using var cmd=c.CreateCommand(); cmd.CommandText=p.InsertSql; var prm=cmd.Parameters.Add("@Rows",SqlDbType.Structured); prm.TypeName=p.TvpTypeName; prm.Value=new RecordRows<T>(rows,p); return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
    private static async ValueTask<int> UpdateSqlDataRecordAsync<T>(SqlConnection c, SqlServerBulkPlan p, IReadOnlyList<T> rows, CancellationToken ct)
    { await using var cmd=c.CreateCommand(); cmd.CommandText=p.UpdateSql; var prm=cmd.Parameters.Add("@Rows",SqlDbType.Structured); prm.TypeName=p.TvpTypeName; prm.Value=new RecordRows<T>(rows,p); return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
    private static async ValueTask<int> DeleteSqlDataRecordAsync<TKey>(SqlConnection c, SqlServerBulkPlan p, IReadOnlyList<TKey> keys, CancellationToken ct)
    { await using var cmd=c.CreateCommand(); cmd.CommandText=p.DeleteSql; var prm=cmd.Parameters.Add("@Rows",SqlDbType.Structured); prm.TypeName=p.KeyTvpTypeName; prm.Value=new KeyRecordRows<TKey>(keys,p); return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }

    private static async ValueTask<int> InsertDataTableAsync<T>(SqlConnection c, SqlServerBulkPlan p, IReadOnlyList<T> rows, CancellationToken ct)
    { await using var cmd=c.CreateCommand(); cmd.CommandText=p.InsertSql; var prm=cmd.Parameters.Add("@Rows",SqlDbType.Structured); prm.TypeName=p.TvpTypeName; prm.Value=p.CreateTable(rows); return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
    private static async ValueTask<int> UpdateDataTableAsync<T>(SqlConnection c, SqlServerBulkPlan p, IReadOnlyList<T> rows, CancellationToken ct)
    { await using var cmd=c.CreateCommand(); cmd.CommandText=p.UpdateSql; var prm=cmd.Parameters.Add("@Rows",SqlDbType.Structured); prm.TypeName=p.TvpTypeName; prm.Value=p.CreateTable(rows); return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
    private static async ValueTask<int> DeleteDataTableAsync<TKey>(SqlConnection c, SqlServerBulkPlan p, IReadOnlyList<TKey> keys, CancellationToken ct)
    { await using var cmd=c.CreateCommand(); cmd.CommandText=p.DeleteSql; var prm=cmd.Parameters.Add("@Rows",SqlDbType.Structured); prm.TypeName=p.KeyTvpTypeName; prm.Value=p.CreateKeyTable(keys); return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }

    private static async ValueTask<int> InsertSqlBulkCopyAsync<T>(SqlConnection c, SqlServerBulkPlan p, IReadOnlyList<T> rows, ForgeBulkOperationOptions o, CancellationToken ct)
    { using var bulk = new SqlBulkCopy(c, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.CheckConstraints, null) { DestinationTableName = p.QuotedTableName, BatchSize = Math.Min(Math.Max(rows.Count,1), o.BatchSize), BulkCopyTimeout = o.CommandTimeoutSeconds, EnableStreaming = true }; var table=p.CreateTable(rows); foreach(var col in p.Columns) bulk.ColumnMappings.Add(col.Name,col.Name); await bulk.WriteToServerAsync(table,ct).ConfigureAwait(false); return rows.Count; }

    private static SqlServerBulkPlan GetPlan<T>(string tableName, string keyColumn, bool includeKeyForColumns)
        => PlanCache.GetOrAdd((typeof(T), tableName, keyColumn + includeKeyForColumns), _ => BuildPlan(typeof(T), tableName, keyColumn, includeKeyForColumns));
    private static SqlServerBulkPlan GetDeletePlan<TKey>(string tableName, string keyColumn)
        => PlanCache.GetOrAdd((typeof(TKey), tableName, keyColumn + "|delete"), _ => BuildDeletePlan(typeof(TKey), tableName, keyColumn));

    private static SqlServerBulkPlan BuildPlan(Type type, string tableName, string keyColumn, bool includeKey)
    {
        var props = type.GetProperties(BindingFlags.Public|BindingFlags.Instance).Where(p=>p.CanRead && IsScalar(p.PropertyType)).ToArray();
        var key = props.FirstOrDefault(p=>string.Equals(p.Name,keyColumn,StringComparison.OrdinalIgnoreCase));
        var columns = props.Where(p=>includeKey || !string.Equals(p.Name,keyColumn,StringComparison.OrdinalIgnoreCase)).Select(p=>new BulkColumn(p.Name,p.PropertyType,p)).ToArray();
        return new SqlServerBulkPlan(type, tableName, keyColumn, key?.PropertyType ?? typeof(int), columns);
    }
    private static SqlServerBulkPlan BuildDeletePlan(Type keyType, string tableName, string keyColumn) => new(keyType, tableName, keyColumn, keyType, Array.Empty<BulkColumn>());

    private static bool IsScalar(Type type) { var t=Nullable.GetUnderlyingType(type)??type; return t.IsPrimitive||t.IsEnum||t==typeof(string)||t==typeof(Guid)||t==typeof(decimal)||t==typeof(DateTime)||t==typeof(DateTimeOffset)||t==typeof(TimeSpan)||t==typeof(byte[]); }
    private static string QuoteTable(string t)=>string.Join('.',t.Split('.',StringSplitOptions.RemoveEmptyEntries).Select(x=>$"[{x.Trim('[',']').Replace("]", "]]", StringComparison.Ordinal)}]"));
    private static string Quote(string s)=>$"[{s.Replace("]", "]]", StringComparison.Ordinal)}]";
    private static object? Norm(object? v, Type type)=>v is null?null:((Nullable.GetUnderlyingType(type)??type).IsEnum?v.ToString():v);
    private static Type StorageType(Type type){ var t=Nullable.GetUnderlyingType(type)??type; return t.IsEnum?typeof(string):t; }
    private static SqlMetaData Meta(string name, Type type){ var t=StorageType(type); if(t==typeof(string)) return new SqlMetaData(name,SqlDbType.NVarChar,-1); if(t==typeof(int)) return new SqlMetaData(name,SqlDbType.Int); if(t==typeof(long)) return new SqlMetaData(name,SqlDbType.BigInt); if(t==typeof(short)) return new SqlMetaData(name,SqlDbType.SmallInt); if(t==typeof(byte)) return new SqlMetaData(name,SqlDbType.TinyInt); if(t==typeof(bool)) return new SqlMetaData(name,SqlDbType.Bit); if(t==typeof(Guid)) return new SqlMetaData(name,SqlDbType.UniqueIdentifier); if(t==typeof(decimal)) return new SqlMetaData(name,SqlDbType.Decimal,18,2); if(t==typeof(double)) return new SqlMetaData(name,SqlDbType.Float); if(t==typeof(float)) return new SqlMetaData(name,SqlDbType.Real); if(t==typeof(DateTime)) return new SqlMetaData(name,SqlDbType.DateTime2); if(t==typeof(DateTimeOffset)) return new SqlMetaData(name,SqlDbType.DateTimeOffset); if(t==typeof(TimeSpan)) return new SqlMetaData(name,SqlDbType.Time); if(t==typeof(byte[])) return new SqlMetaData(name,SqlDbType.VarBinary,-1); return new SqlMetaData(name,SqlDbType.NVarChar,-1); }
    private static void Set(SqlDataRecord r,int i,object? v,Type type){ if(v is null or DBNull){r.SetDBNull(i);return;} var t=StorageType(type); if(t==typeof(string)){r.SetString(i,Convert.ToString(v)!);return;} if(t==typeof(int)){r.SetInt32(i,Convert.ToInt32(v));return;} if(t==typeof(long)){r.SetInt64(i,Convert.ToInt64(v));return;} if(t==typeof(short)){r.SetInt16(i,Convert.ToInt16(v));return;} if(t==typeof(byte)){r.SetByte(i,Convert.ToByte(v));return;} if(t==typeof(bool)){r.SetBoolean(i,Convert.ToBoolean(v));return;} if(t==typeof(Guid)){r.SetGuid(i,v is Guid g?g:Guid.Parse(v.ToString()!));return;} if(t==typeof(decimal)){r.SetDecimal(i,Convert.ToDecimal(v));return;} if(t==typeof(double)){r.SetDouble(i,Convert.ToDouble(v));return;} if(t==typeof(float)){r.SetFloat(i,Convert.ToSingle(v));return;} if(t==typeof(DateTime)){r.SetDateTime(i,Convert.ToDateTime(v));return;} r.SetValue(i,v); }

    private sealed record BulkColumn(string Name, Type Type, PropertyInfo? Property);
    internal sealed class SqlServerBulkPlan
    {
        public Type EntityType { get; } public string TableName { get; } public string QuotedTableName { get; } public string KeyColumn { get; } public Type KeyType { get; } public BulkColumn[] Columns { get; } public string TvpTypeName { get; } public string KeyTvpTypeName { get; } public SqlMetaData[] MetaData { get; } public SqlMetaData[] KeyMetaData { get; } public string InsertSql { get; } public string UpdateSql { get; } public string DeleteSql { get; }
        public SqlServerBulkPlan(Type entityType,string tableName,string keyColumn,Type keyType,BulkColumn[] columns){EntityType=entityType;TableName=tableName;QuotedTableName=QuoteTable(tableName);KeyColumn=keyColumn;KeyType=keyType;Columns=columns;TvpTypeName=entityType.Name+"TableType";KeyTvpTypeName=entityType.Name+"KeyTableType";MetaData=columns.Select(c=>Meta(c.Name,c.Type)).ToArray();KeyMetaData=new[]{Meta(keyColumn,keyType)};InsertSql=BuildInsert();UpdateSql=BuildUpdate();DeleteSql=BuildDelete();}
        private string BuildInsert(){var names=Columns.Select(c=>Quote(c.Name)).ToArray();return $"INSERT INTO {QuotedTableName} ({string.Join(", ",names)}) SELECT {string.Join(", ",names)} FROM @Rows";}
        private string BuildUpdate(){var set=string.Join(", ",Columns.Where(c=>!string.Equals(c.Name,KeyColumn,StringComparison.OrdinalIgnoreCase)).Select(c=>$"T.{Quote(c.Name)} = S.{Quote(c.Name)}"));return $"MERGE {QuotedTableName} AS T USING @Rows AS S ON T.{Quote(KeyColumn)} = S.{Quote(KeyColumn)} WHEN MATCHED THEN UPDATE SET {set};";}
        private string BuildDelete()=> $"DELETE T FROM {QuotedTableName} T INNER JOIN @Rows R ON T.{Quote(KeyColumn)} = R.{Quote(KeyColumn)}";
        public object? GetValue(object entity,int i)=>Columns[i].Property?.GetValue(entity);
        public DataTable CreateTable<T>(IReadOnlyList<T> rows){var dt=new DataTable();foreach(var c in Columns)dt.Columns.Add(c.Name,StorageType(c.Type));for(var r=0;r<rows.Count;r++){var row=dt.NewRow();for(var i=0;i<Columns.Length;i++)row[i]=Norm(GetValue(rows[r]!,i),Columns[i].Type)??DBNull.Value;dt.Rows.Add(row);}return dt;}
        public DataTable CreateKeyTable<TKey>(IReadOnlyList<TKey> keys){var dt=new DataTable();dt.Columns.Add(KeyColumn,StorageType(KeyType));for(var i=0;i<keys.Count;i++){var row=dt.NewRow();row[0]=Norm(keys[i],KeyType)??DBNull.Value;dt.Rows.Add(row);}return dt;}
    }
    private sealed class RecordRows<T> : IEnumerable<SqlDataRecord>{private readonly IReadOnlyList<T> _rows;private readonly SqlServerBulkPlan _p;public RecordRows(IReadOnlyList<T> rows,SqlServerBulkPlan p){_rows=rows;_p=p;}public IEnumerator<SqlDataRecord> GetEnumerator(){for(var r=0;r<_rows.Count;r++){var rec=new SqlDataRecord(_p.MetaData);for(var c=0;c<_p.Columns.Length;c++)Set(rec,c,Norm(_p.GetValue(_rows[r]!,c),_p.Columns[c].Type),_p.Columns[c].Type);yield return rec;}}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();}
    private sealed class KeyRecordRows<TKey> : IEnumerable<SqlDataRecord>{private readonly IReadOnlyList<TKey> _keys;private readonly SqlServerBulkPlan _p;public KeyRecordRows(IReadOnlyList<TKey> keys,SqlServerBulkPlan p){_keys=keys;_p=p;}public IEnumerator<SqlDataRecord> GetEnumerator(){for(var i=0;i<_keys.Count;i++){var rec=new SqlDataRecord(_p.KeyMetaData);Set(rec,0,Norm(_keys[i],_p.KeyType),_p.KeyType);yield return rec;}}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();}
}
