using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.Rag;

public sealed class DeterministicEmbeddingProvider : IForgeEmbeddingProvider
{
    private const int Dimensions = 64;

    /// <summary>
    /// Executes the EmbedAsync operation.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the EmbedAsync operation.</returns>
    public ValueTask<float[]> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var vector = new float[Dimensions];

        foreach (var ch in text ?? string.Empty)
        {
            var idx = ((int)ch) % vector.Length;
            vector[idx] += 1f;
        }

        var length = MathF.Sqrt(vector.Sum(x => x * x));

        if (length > 0)
        {
            for (var i = 0; i < vector.Length; i++)
                vector[i] /= length;
        }

        return ValueTask.FromResult(vector);
    }
}
