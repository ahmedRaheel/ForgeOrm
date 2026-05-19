using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ForgeORM.Core.Compiled;

namespace ForgeORM.Benchmarks;

[MemoryDiagnoser]
public class OrmComparisonBenchmarks
{
    private readonly List<BenchProduct> _rows = Enumerable.Range(1, 10_000)
        .Select(i => new BenchProduct { Id = i, Name = "Product " + i, Price = i })
        .ToList();

    [Benchmark(Baseline = true)]
    public int ReflectionMappingBaseline()
    {
        var total = 0;
        foreach (var row in _rows)
        {
            total += (int)(row.GetType().GetProperty(nameof(BenchProduct.Id))!.GetValue(row) ?? 0);
        }
        return total;
    }

    [Benchmark]
    public int ForgeCompiledAccessor()
    {
        var plan = ForgeCompiledPlanCache.For<BenchProduct>();
        var id = plan.Properties.First(x => x.Name == nameof(BenchProduct.Id));
        var total = 0;

        foreach (var row in _rows)
        {
            total += (int)(id.Getter(row) ?? 0);
        }

        return total;
    }

    [Benchmark]
    public string ForgeCompiledSqlPlan()
    {
        return ForgeCompiledPlanCache.For<BenchProduct>().InsertSql;
    }
}

public sealed class BenchProduct
{
    public int Id { get; set; }
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
