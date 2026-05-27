using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Low-allocation scalar parameter container used by hot QueryFirst/GetById paths.
/// It avoids anonymous-object property scanning while keeping the public API simple.
/// </summary>
public interface IForgeDirectScalarParameter
{
    string Name { get; }
    Type ValueType { get; }
    object? BoxedValue { get; }
}

/// <summary>Typed scalar parameter. Prefer this over anonymous objects in ultra-hot paths.</summary>
public readonly struct ForgeScalarParameter<T> : IForgeDirectScalarParameter
{
    public ForgeScalarParameter(string name, T value)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Value" : name;
        Value = value;
    }

    public string Name { get; }
    public T Value { get; }
    public Type ValueType => typeof(T);
    public object? BoxedValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Value;
    }
}
