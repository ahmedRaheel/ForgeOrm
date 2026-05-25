using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public static class ForgeVectorizedMath
{
    public static float Sum(ReadOnlySpan<float> values)
    {
        if (!Vector.IsHardwareAccelerated || values.Length < Vector<float>.Count)
        {
            var total = 0f;
            foreach (var value in values) total += value;
            return total;
        }

        var vectorTotal = Vector<float>.Zero;
        var i = 0;
        for (; i <= values.Length - Vector<float>.Count; i += Vector<float>.Count)
        {
            vectorTotal += new Vector<float>(values.Slice(i, Vector<float>.Count));
        }

        var result = 0f;
        for (var j = 0; j < Vector<float>.Count; j++) result += vectorTotal[j];
        for (; i < values.Length; i++) result += values[i];
        return result;
    }

    public static float CosineSimilarity(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        var length = Math.Min(left.Length, right.Length);
        if (length == 0) return 0;

        var dot = 0f;
        var leftMagnitude = 0f;
        var rightMagnitude = 0f;

        for (var i = 0; i < length; i++)
        {
            dot += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        return leftMagnitude == 0 || rightMagnitude == 0
            ? 0
            : dot / (float)(Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}
