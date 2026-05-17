using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Graph;

/// <summary>
/// Compares objects by reference instead of value.
/// </summary>
public sealed class ReferenceEqualityComparer : IEqualityComparer<object>
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static ReferenceEqualityComparer Instance { get; } = new();

    private ReferenceEqualityComparer()
    {
    }

    /// <inheritdoc />
    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

    /// <inheritdoc />
    public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
}
