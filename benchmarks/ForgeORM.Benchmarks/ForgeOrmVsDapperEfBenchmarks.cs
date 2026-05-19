using System.Data;
using System.Data.Common;
using BenchmarkDotNet.Attributes;
using ForgeORM.Core;

namespace ForgeORM.Benchmarks;

/// <summary>
/// Micro-benchmarks for the hot paths ForgeORM owns directly: reader materialization,
/// parameter binding, paging command shape, and cached metadata. Database-specific
/// integration benchmarks can be added by supplying a real SQL Server/PostgreSQL connection string.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 8)]
public class ForgeOrmVsDapperEfBenchmarks
{
    private readonly FakeProductReader _reader = new(10_000);
    private readonly BenchProductParameters _parameters = new() { Id = 42, Name = "Keyboard", Price = 99.95m, Active = true };

    [Benchmark(Baseline = true)]
    public int ReflectionReaderBaseline()
    {
        var total = 0;
        _reader.Reset();
        while (_reader.Read())
        {
            var p = new BenchProduct();
            typeof(BenchProduct).GetProperty(nameof(BenchProduct.Id))!.SetValue(p, _reader.GetInt32(0));
            typeof(BenchProduct).GetProperty(nameof(BenchProduct.Name))!.SetValue(p, _reader.GetString(1));
            typeof(BenchProduct).GetProperty(nameof(BenchProduct.Price))!.SetValue(p, _reader.GetDecimal(2));
            total += p.Id;
        }
        return total;
    }

    [Benchmark]
    public int ForgeORM_MsilReader()
    {
        var materializer = GetMsilReader();
        var total = 0;
        _reader.Reset();
        while (_reader.Read())
            total += materializer(_reader).Id;
        return total;
    }

    [Benchmark]
    public int ForgeORM_MsilParameterBinder()
    {
        using var command = new FakeDbCommand();
        var created = 0;
        for (var i = 0; i < 10_000; i++)
        {
            command.Parameters.Clear();
            ForgeAdo.CreateCommand(new FakeDbConnection(), "select * from Products where Id = @Id and Active = @Active", _parameters);
            created += 4;
        }
        return created;
    }

    private static Func<DbDataReader, BenchProduct> GetMsilReader()
    {
        using var reader = new FakeProductReader(1);
        return typeof(ForgeAdo)
            .Assembly
            .GetType("ForgeORM.Core.ForgeIlMaterializerCache")!
            .GetMethod("GetOrCreate", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(BenchProduct))
            .Invoke(null, new object[] { reader }) as Func<DbDataReader, BenchProduct>
            ?? throw new InvalidOperationException("Unable to create ForgeORM MSIL materializer.");
    }
}

public sealed class BenchProductParameters
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool Active { get; set; }
}

internal sealed class FakeProductReader : DbDataReader
{
    private readonly int _count;
    private int _row = -1;
    public FakeProductReader(int count) => _count = count;
    public void Reset() => _row = -1;
    public override bool Read() => ++_row < _count;
    public override int FieldCount => 3;
    public override string GetName(int ordinal) => ordinal switch { 0 => "Id", 1 => "Name", 2 => "Price", _ => throw new IndexOutOfRangeException() };
    public override Type GetFieldType(int ordinal) => ordinal switch { 0 => typeof(int), 1 => typeof(string), 2 => typeof(decimal), _ => typeof(object) };
    public override int GetOrdinal(string name) => name switch { "Id" => 0, "Name" => 1, "Price" => 2, _ => -1 };
    public override int GetInt32(int ordinal) => _row + 1;
    public override string GetString(int ordinal) => "Product " + (_row + 1);
    public override decimal GetDecimal(int ordinal) => _row + 1;
    public override object GetValue(int ordinal) => ordinal switch { 0 => GetInt32(ordinal), 1 => GetString(ordinal), 2 => GetDecimal(ordinal), _ => DBNull.Value };
    public override bool IsDBNull(int ordinal) => false;
    public override bool HasRows => true;
    public override int RecordsAffected => 0;
    public override bool IsClosed => false;
    public override int Depth => 0;
    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));
    public override bool NextResult() => false;
    public override int GetValues(object[] values) { for (var i = 0; i < FieldCount; i++) values[i] = GetValue(i); return FieldCount; }
    public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);
    public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
    public override char GetChar(int ordinal) => (char)GetValue(ordinal);
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
    public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
    public override DateTime GetDateTime(int ordinal) => DateTime.UtcNow;
    public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal));
    public override System.Collections.IEnumerator GetEnumerator() => throw new NotSupportedException();
    public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal));
    public override Guid GetGuid(int ordinal) => Guid.NewGuid();
    public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal));
    public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal));
}

internal sealed class FakeDbConnection : DbConnection
{
    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => "Fake";
    public override string DataSource => "Fake";
    public override string ServerVersion => "1";
    public override ConnectionState State => ConnectionState.Open;
    public override void ChangeDatabase(string databaseName) { }
    public override void Close() { }
    public override void Open() { }
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
    protected override DbCommand CreateDbCommand() => new FakeDbCommand { Connection = this };
}

internal sealed class FakeDbCommand : DbCommand
{
    private readonly FakeParameterCollection _parameters = new();
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }
    public override void Cancel() { }
    public override int ExecuteNonQuery() => 0;
    public override object? ExecuteScalar() => null;
    public override void Prepare() { }
    protected override DbParameter CreateDbParameter() => new FakeDbParameter();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
}

internal sealed class FakeDbParameter : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; }
    public override bool IsNullable { get; set; }
    public override string ParameterName { get; set; } = string.Empty;
    public override string SourceColumn { get; set; } = string.Empty;
    public override object? Value { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override int Size { get; set; }
    public override void ResetDbType() { }
}

internal sealed class FakeParameterCollection : DbParameterCollection
{
    private readonly List<object> _items = new();
    public override int Count => _items.Count;
    public override object SyncRoot => this;
    public override int Add(object value) { _items.Add(value); return _items.Count - 1; }
    public override void AddRange(Array values) { foreach (var value in values) Add(value!); }
    public override void Clear() => _items.Clear();
    public override bool Contains(object value) => _items.Contains(value);
    public override bool Contains(string value) => _items.OfType<DbParameter>().Any(x => x.ParameterName == value);
    public override void CopyTo(Array array, int index) => _items.ToArray().CopyTo(array, index);
    public override System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();
    public override int IndexOf(object value) => _items.IndexOf(value);
    public override int IndexOf(string parameterName) => _items.FindIndex(x => x is DbParameter p && p.ParameterName == parameterName);
    public override void Insert(int index, object value) => _items.Insert(index, value);
    public override void Remove(object value) => _items.Remove(value);
    public override void RemoveAt(int index) => _items.RemoveAt(index);
    public override void RemoveAt(string parameterName) { var i = IndexOf(parameterName); if (i >= 0) RemoveAt(i); }
    protected override DbParameter GetParameter(int index) => (DbParameter)_items[index];
    protected override DbParameter GetParameter(string parameterName) => (DbParameter)_items[IndexOf(parameterName)];
    protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
    protected override void SetParameter(string parameterName, DbParameter value) { var i = IndexOf(parameterName); if (i >= 0) _items[i] = value; else Add(value); }
}
