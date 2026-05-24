namespace ForgeORM.VectorSearch;

public static class ForgeVectorMath
{
    /// <summary>
    /// Executes the CosineSimilarity operation.
    /// </summary>
    /// <param name="a">The a value.</param>
    /// <param name="b">The b value.</param>
    /// <returns>The result of the CosineSimilarity operation.</returns>
    public static double CosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        if (a.Count != b.Count) throw new ArgumentException("Vector dimensions must match.");
        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Count; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return magA == 0 || magB == 0 ? 0 : dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
