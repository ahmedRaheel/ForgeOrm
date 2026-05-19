using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Graph;

/// <summary>
/// Tracks temporary object identities and database generated keys during graph operations.
/// </summary>
public sealed class ForgeGraphIdentityMap
{
    private readonly Dictionary<ReferenceKey, object?> _databaseKeys = new();

    /// <summary>
    /// Stores a database key for the supplied entity instance.
    /// </summary>
    public void SetDatabaseKey(object entity, object? databaseKey)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _databaseKeys[new ReferenceKey(entity)] = databaseKey;
    }

    /// <summary>
    /// Attempts to get the database key for the supplied entity instance.
    /// </summary>
    public bool TryGetDatabaseKey(object entity, out object? databaseKey)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _databaseKeys.TryGetValue(new ReferenceKey(entity), out databaseKey);
    }

    /// <summary>
    /// Gets the database key for the supplied entity instance, or <c>null</c> when no key has been registered.
    /// </summary>
    public object? GetDatabaseKeyOrDefault(object entity)
    {
        return TryGetDatabaseKey(entity, out var key) ? key : null;
    }

    private readonly record struct ReferenceKey(object Value)
    {
        public bool Equals(ReferenceKey other) => ReferenceEquals(Value, other.Value);

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(Value);
    }
}
