using System.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ForgeORM.Core;
using ForgeORM.Core.Compiled;
using ForgeORM.Core.Performance;

namespace ForgeORM.Benchmarks;

[MemoryDiagnoser]
public class OrmComparisonBenchmarks
{
    private DataTable _table = default!;
    private readonly List<BenchProduct> _rows = Enumerable.Range(1, 10_000)
        .Select(i => new BenchProduct { Id = i, Code = "P" + i, Name = "Product " + i, Price = i })
        .ToList();

    [GlobalSetup]
    public void Setup()
    {
        _table = new DataTable("Products");
        _table.Columns.Add("Id", typeof(int));
        _table.Columns.Add("Code", typeof(string));
        _table.Columns.Add("Name", typeof(string));
        _table.Columns.Add("Price", typeof(decimal));

        foreach (var row in _rows)
            _table.Rows.Add(row.Id, row.Code, row.Name, row.Price);

        ForgeRuntimeEntityMetadataCache.PreWarm(typeof(BenchProduct));
    }

    [Benchmark(Baseline = true)]
    public int ReflectionPropertyReadBaseline()
    {
        var property = typeof(BenchProduct).GetProperty(nameof(BenchProduct.Id))!;
        var total = 0;
        foreach (var row in _rows)
            total += (int)(property.GetValue(row) ?? 0);
        return total;
    }

    [Benchmark]
    public int MsilCompiledAccessor()
    {
        var plan = ForgeCompiledPlanCache.For<BenchProduct>();
        var id = plan.Properties.First(x => x.Name == nameof(BenchProduct.Id));
        var total = 0;

        foreach (var row in _rows)
            total += (int)(id.Getter(row) ?? 0);

        return total;
    }

    [Benchmark]
    public int ManualReaderMapping()
    {
        using var reader = _table.CreateDataReader();
        var total = 0;
        while (reader.Read())
        {
            var product = new BenchProduct
            {
                Id = reader.GetInt32(0),
                Code = reader.GetString(1),
                Name = reader.GetString(2),
                Price = reader.GetDecimal(3)
            };
            total += product.Id;
        }
        return total;
    }

    [Benchmark]
    public int ForgeMsilReaderMaterializer()
    {
        using var reader = _table.CreateDataReader();
        var materializer = ForgeRuntimeReaderCompiler.GetOrCreate<BenchProduct>(reader);
        var total = 0;
        while (reader.Read())
            total += materializer(reader).Id;
        return total;
    }

    [Benchmark]
    public string ForgeCachedInsertSql()
        => ForgeCompiledPlanCache.For<BenchProduct>().InsertSql;

    [Benchmark]
    public object ForgeCacheStats()
    {
        using var reader = _table.CreateDataReader();
        _ = ForgeRuntimeReaderCompiler.GetOrCreate<BenchProduct>(reader);
        _ = ForgeRuntimeQueryPlanCache.For<BenchProduct>("SELECT Id, Code, Name, Price FROM Products");
        return new { queryPlans = ForgeRuntimeQueryPlanCache.Count };
    }
}

public sealed class BenchProduct
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
