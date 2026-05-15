namespace ForgeORM.DataFrame;

public abstract class ForgeAgg
{
    public abstract string Name { get; }
    public abstract object? Compute(IEnumerable<object?> values);

    public static ForgeAgg Count() => new CountAgg();
    public static ForgeAgg Sum() => new SumAgg();
    public static ForgeAgg Avg() => new AvgAgg();
    public static ForgeAgg Min() => new MinAgg();
    public static ForgeAgg Max() => new MaxAgg();
    public static ForgeAgg Median() => new PercentileAgg(0.5m, "Median");
    public static ForgeAgg Percentile(decimal percentile) => new PercentileAgg(percentile, "P" + (int)(percentile * 100));

    private sealed class CountAgg : ForgeAgg
    {
        public override string Name => "Count";
        public override object Compute(IEnumerable<object?> values) => values.Count(v => v is not null and not DBNull);
    }

    private sealed class SumAgg : ForgeAgg
    {
        public override string Name => "Sum";
        public override object Compute(IEnumerable<object?> values) => values.Select(ForgeDataFrame.ToDecimal).Where(x => x.HasValue).Sum(x => x!.Value);
    }

    private sealed class AvgAgg : ForgeAgg
    {
        public override string Name => "Avg";
        public override object? Compute(IEnumerable<object?> values)
        {
            var list = values.Select(ForgeDataFrame.ToDecimal).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            return list.Count == 0 ? null : list.Average();
        }
    }

    private sealed class MinAgg : ForgeAgg
    {
        public override string Name => "Min";
        public override object? Compute(IEnumerable<object?> values)
        {
            var list = values.Where(x => x is not null and not DBNull).Cast<IComparable>().ToList();
            return list.Count == 0 ? null : list.Min();
        }
    }

    private sealed class MaxAgg : ForgeAgg
    {
        public override string Name => "Max";
        public override object? Compute(IEnumerable<object?> values)
        {
            var list = values.Where(x => x is not null and not DBNull).Cast<IComparable>().ToList();
            return list.Count == 0 ? null : list.Max();
        }
    }

    private sealed class PercentileAgg(decimal percentile, string name) : ForgeAgg
    {
        public override string Name => name;
        public override object? Compute(IEnumerable<object?> values)
        {
            var list = values.Select(ForgeDataFrame.ToDecimal).Where(x => x.HasValue).Select(x => x!.Value).OrderBy(x => x).ToList();
            return list.Count == 0 ? null : ForgeDataFrame.Percentile(list, percentile);
        }
    }
}
