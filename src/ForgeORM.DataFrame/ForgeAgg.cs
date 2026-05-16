namespace ForgeORM.DataFrame;

public abstract class ForgeAgg
{
    public abstract string Name { get; }
    /// <summary>
    /// Initializes or executes the Compute operation.
    /// </summary>
    /// <param name="values">The values value.</param>
    /// <returns>The operation result.</returns>
    public abstract object? Compute(IEnumerable<object?> values);

    /// <summary>
    /// Initializes or executes the Count operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public static ForgeAgg Count() => new CountAgg();
    /// <summary>
    /// Initializes or executes the Sum operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public static ForgeAgg Sum() => new SumAgg();
    /// <summary>
    /// Initializes or executes the Avg operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public static ForgeAgg Avg() => new AvgAgg();
    /// <summary>
    /// Initializes or executes the Min operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public static ForgeAgg Min() => new MinAgg();
    /// <summary>
    /// Initializes or executes the Max operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public static ForgeAgg Max() => new MaxAgg();
    /// <summary>
    /// Initializes or executes the Median operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public static ForgeAgg Median() => new PercentileAgg(0.5m, "Median");
    /// <summary>
    /// Initializes or executes the Percentile operation.
    /// </summary>
    /// <param name="percentile">The percentile value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeAgg Percentile(decimal percentile) => new PercentileAgg(percentile, "P" + (int)(percentile * 100));

    private sealed class CountAgg : ForgeAgg
    {
        public override string Name => "Count";
        /// <summary>
        /// Initializes or executes the Compute operation.
        /// </summary>
        /// <param name="values">The values value.</param>
        /// <returns>The operation result.</returns>
        public override object Compute(IEnumerable<object?> values) => values.Count(v => v is not null and not DBNull);
    }

    private sealed class SumAgg : ForgeAgg
    {
        public override string Name => "Sum";
        /// <summary>
        /// Initializes or executes the Compute operation.
        /// </summary>
        /// <param name="values">The values value.</param>
        /// <returns>The operation result.</returns>
        public override object Compute(IEnumerable<object?> values) => values.Select(ForgeDataFrame.ToDecimal).Where(x => x.HasValue).Sum(x => x!.Value);
    }

    private sealed class AvgAgg : ForgeAgg
    {
        public override string Name => "Avg";
        /// <summary>
        /// Initializes or executes the Compute operation.
        /// </summary>
        /// <param name="values">The values value.</param>
        /// <returns>The operation result.</returns>
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
        /// Initializes or executes the Compute operation.
        /// </summary>
        /// <param name="values">The values value.</param>
        /// <returns>The operation result.</returns>
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
        /// Initializes or executes the Compute operation.
        /// </summary>
        /// <param name="values">The values value.</param>
        /// <returns>The operation result.</returns>
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
        /// Initializes or executes the Compute operation.
        /// </summary>
        /// <param name="values">The values value.</param>
        /// <returns>The operation result.</returns>
        public override object? Compute(IEnumerable<object?> values)
        {
            var list = values.Select(ForgeDataFrame.ToDecimal).Where(x => x.HasValue).Select(x => x!.Value).OrderBy(x => x).ToList();
            return list.Count == 0 ? null : ForgeDataFrame.Percentile(list, percentile);
        }
    }
}
