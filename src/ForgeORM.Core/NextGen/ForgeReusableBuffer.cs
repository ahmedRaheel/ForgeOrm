using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed class ForgeReusableBuffer<T> : IDisposable
{
    private readonly ArrayPool<T> _pool;
    public T[] Buffer { get; }
    public int Length { get; }

    public ForgeReusableBuffer(int length, ArrayPool<T>? pool = null)
    {
        Length = length;
        _pool = pool ?? ArrayPool<T>.Shared;
        Buffer = _pool.Rent(length);
    }

    public Memory<T> Memory => Buffer.AsMemory(0, Length);
    public Span<T> Span => Buffer.AsSpan(0, Length);

    public void Dispose() => _pool.Return(Buffer, clearArray: true);
}
