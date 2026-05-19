using System.Buffers;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Small pooled SQL builder to avoid string-concat loops in hot SQL generation paths.
/// </summary>
internal ref struct ForgePooledSqlBuilder
{
    private char[] _buffer;
    private int _position;

    public ForgePooledSqlBuilder(int initialCapacity)
    {
        _buffer = ArrayPool<char>.Shared.Rent(Math.Max(initialCapacity, 256));
        _position = 0;
    }

    public void Append(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        Ensure(value.Length);
        value.AsSpan().CopyTo(_buffer.AsSpan(_position));
        _position += value.Length;
    }

    public void Append(char value)
    {
        Ensure(1);
        _buffer[_position++] = value;
    }

    public override string ToString() => new(_buffer, 0, _position);

    public void Dispose()
    {
        var rented = _buffer;
        _buffer = Array.Empty<char>();
        _position = 0;
        ArrayPool<char>.Shared.Return(rented, clearArray: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Ensure(int additional)
    {
        if (_position + additional <= _buffer.Length)
            return;

        var next = ArrayPool<char>.Shared.Rent(Math.Max(_buffer.Length * 2, _position + additional));
        _buffer.AsSpan(0, _position).CopyTo(next);
        ArrayPool<char>.Shared.Return(_buffer, clearArray: false);
        _buffer = next;
    }
}
