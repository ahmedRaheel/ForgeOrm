using System.Runtime.CompilerServices;
using System.Text;

namespace ForgeORM.Core;

/// <summary>
/// Non-cryptographic hot-path fingerprinting for SQL/query shapes. This replaces SHA256 in caches where security is not required.
/// Uses FNV-1a 64-bit over whitespace-normalized text to avoid expensive allocations and cryptographic hashing.
/// </summary>
internal static class ForgeFastHash
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong HashSql(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0UL;

        var hash = Offset;
        var pendingSpace = false;

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace)
            {
                hash ^= (byte)' ';
                hash *= Prime;
                pendingSpace = false;
            }

            hash ^= char.ToUpperInvariant(c);
            hash *= Prime;
        }

        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string FingerprintSql(string? value)
        => HashSql(value).ToString("X16", System.Globalization.CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string FingerprintReaderShape(DbDataReaderShape shape)
    {
        var hash = Offset;
        Add(ref hash, shape.ProviderName);
        Add(ref hash, shape.TargetType.FullName ?? shape.TargetType.Name);
        for (var i = 0; i < shape.Columns.Length; i++)
        {
            ref readonly var col = ref shape.Columns[i];
            Add(ref hash, col.Name);
            Add(ref hash, col.ClrType.FullName ?? col.ClrType.Name);
            Add(ref hash, col.DbTypeName);
            Add(ref hash, col.AllowDBNull ? "1" : "0");
        }
        return hash.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(ref ulong hash, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            hash ^= 0;
            hash *= Prime;
            return;
        }

        for (var i = 0; i < value.Length; i++)
        {
            hash ^= char.ToUpperInvariant(value[i]);
            hash *= Prime;
        }
        hash ^= (byte)'|';
        hash *= Prime;
    }
}
