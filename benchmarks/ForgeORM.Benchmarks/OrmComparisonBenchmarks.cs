using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ForgeORM.Core.Compiled;

namespace ForgeORM.Benchmarks;

[MemoryDiagnoser]
public class OrmComparisonBenchmarks
{
    private readonly List<BenchProductAccess> _rows = Enumerable.Range(1, 10_000)
        .Select(i => new BenchProductAccess { Id = i, Name = "Product " + i, Price = i })
        .ToList();

    [Benchmark(Baseline = true)]
    public int ReflectionMappingBaseline()
    {
        var total = 0;
        foreach (var row in _rows)
        {
            total += (int)(row.GetType().GetProperty(nameof(BenchProductAccess.Id))!.GetValue(row) ?? 0);
        }
        return total;
    }

    [Benchmark]
    public int ForgeCompiledAccessor()
    {
        var plan = ForgeCompiledPlanCache.For<BenchProductAccess>();
        var id = plan.Properties.First(x => x.Name == nameof(BenchProductAccess.Id));
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
        return ForgeCompiledPlanCache.For<BenchProductAccess>().InsertSql;
    }
}

public sealed class BenchProductAccess
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
