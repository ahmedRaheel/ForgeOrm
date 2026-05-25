using System.Buffers;
using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

internal readonly struct PooledArray<T> : IDisposable
{
    public T[] Buffer { get; }
    public PooledArray(T[] buffer) => Buffer = buffer;
    public void Dispose() => ArrayPool<T>.Shared.Return(Buffer, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
}
