namespace ForgeORM.DataFrame;

public abstract class ForgeAgg
{
    /// <summary>
    /// Executes the Compute operation.
    /// </summary>
    /// <param name="values">The values value.</param>
    /// <returns>The result of the Compute operation.</returns>
    public abstract string Name { get; }
    /// <summary>
    /// Executes the Compute operation.
    /// </summary>
    /// <param name="values">The values value.</param>
    /// <returns>The result of the Compute operation.</returns>
    public abstract object? Compute(IEnumerable<object?> values);

    /// <summary>
    /// Executes the Count operation.
    /// </summary>
    /// <returns>The result of the Count operation.</returns>
    public static ForgeAgg Count() => new CountAgg();
    /// <summary>
    /// Executes the Sum operation.
    /// </summary>
    /// <returns>The result of the Sum operation.</returns>
    public static ForgeAgg Sum() => new SumAgg();
    /// <summary>
    /// Executes the Avg operation.
    /// </summary>
    /// <returns>The result of the Avg operation.</returns>
    public static ForgeAgg Avg() => new AvgAgg();
    /// <summary>
    /// Executes the Min operation.
    /// </summary>
    /// <returns>The result of the Min operation.</returns>
    public static ForgeAgg Min() => new MinAgg();
    /// <summary>
    /// Executes the Max operation.
    /// </summary>
    /// <returns>The result of the Max operation.</returns>
    public static ForgeAgg Max() => new MaxAgg();
    /// <summary>
    /// Executes the Median operation.
    /// </summary>
    /// <returns>The result of the Median operation.</returns>
    public static ForgeAgg Median() => new PercentileAgg(0.5m, "Median");
    /// <summary>
    /// Executes the Percentile operation.
    /// </summary>
    /// <param name="percentile">The percentile value.</param>
    /// <returns>The result of the Percentile operation.</returns>
    public static ForgeAgg Percentile(decimal percentile) => new PercentileAgg(percentile, "P" + (int)(percentile * 100));

    private sealed class CountAgg : ForgeAgg
    {
        public override string Name => "Count";
        /// <summary>
        /// Executes the Compute operation.
        /// </summary>
        /// <param name="values">The values value.</param>
        /// <returns>The result of the Compute operation.</returns>
        public override object Compute(IEnumerable<object?> values) => values.Count(v => v is not null and not DBNull);
    }

    private sealed class SumAgg : ForgeAgg
    {
        public override string Name => "Sum";
        /// <summary>
        /// Executes the Compute operation.
        /// </summary>
        /// <param name="values">The values value.</param>
        /// <returns>The result of the Compute operation.</returns>
        public override object Compute(IEnumerable<object?> values) => values.Select(ForgeDataFrame.ToDecimal).Where(x => x.HasValue).Sum(x => x!.Value);
    }

    private sealed class AvgAgg : ForgeAgg
    {
        public override string Name => "Avg";
        /// <summary>
        /// Executes the Compute operation.
        /// </summary>
        /// <param name="values">The values value.</param>
        /// <returns>The result of the Compute operation.</returns>
        public override object? Compute(IEnumerable<object?> values)
        {
            var list = values.Select(ForgeDataFrame.ToDecimal).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            return list.Count == 0 ? null : list.Average();
        }
    }

    private sealed class MinAgg : ForgeAgg
    {
        public override string Name => "Min";
        /// <summary>
        /// Executes the Compute operation.
        /// </summary>
        /// <param name="values">The values value.</param>
        /// <returns>The result of the Compute operation.</returns>
        public override object? Compute(IEnumerable<object?> values)
        {
            var list = values.Where(x => x is not null and not DBNull).Cast<IComparable>().ToList();
            return list.Count == 0 ? null : list.Min();
        }
    }

    private sealed class MaxAgg : ForgeAgg
    {
        public override string Name => "Max";
        /// <summary>
        /// Executes the Compute operation.
        /// </summary>
        /// <param name="values">The values value.</param>
        /// <returns>The result of the Compute operation.</returns>
        public override object? Compute(IEnumerable<object?> values)
        {
            var list = values.Where(x => x is not null and not DBNull).Cast<IComparable>().ToList();
            return list.Count == 0 ? null : list.Max();
        }
    }

    private sealed class PercentileAgg(decimal percentile, string name) : ForgeAgg
    {
        public override string Name => name;
        /// <summary>
        /// Executes the Compute operation.
        /// </summary>
        /// <param name="values">The values value.</param>
        /// <returns>The result of the Compute operation.</returns>
        public override object? Compute(IEnumerable<object?> values)
        {
            var list = values.Select(ForgeDataFrame.ToDecimal).Where(x => x.HasValue).Select(x => x!.Value).OrderBy(x => x).ToList();
            return list.Count == 0 ? null : ForgeDataFrame.Percentile(list, percentile);
        }
    }
}
