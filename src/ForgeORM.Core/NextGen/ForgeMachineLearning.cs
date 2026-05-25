using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public static class ForgeMachineLearning
{
    public static IReadOnlyList<decimal> MovingAverage(IReadOnlyList<decimal> values, int window)
    {
        var result = new List<decimal>();
        for (var i = 0; i < values.Count; i++)
        {
            var start = Math.Max(0, i - window + 1);
            var slice = values.Skip(start).Take(i - start + 1).ToArray();
            result.Add(slice.Average());
        }
        return result;
    }

    public static IReadOnlyList<int> DetectAnomalies(IReadOnlyList<decimal> values, decimal thresholdMultiplier = 2m)
    {
        if (values.Count == 0) return [];
        var avg = values.Average();
        var variance = values.Average(v => (v - avg) * (v - avg));
        var std = (decimal)Math.Sqrt((double)variance);
        var threshold = std * thresholdMultiplier;

        return values.Select((v, i) => Math.Abs(v - avg) > threshold ? i : -1)
            .Where(i => i >= 0)
            .ToList();
    }
}
