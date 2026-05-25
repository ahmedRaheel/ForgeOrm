using System.Data;
using System.Reflection;

namespace ForgeORM.Mapping;

public interface IForgeObjectMapper
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="record">The record value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="record">The record value.</param>
    /// <returns>The result of the T operation.</returns>
    T Map<T>(IDataRecord record);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="reader">The reader value.</param>
    /// <returns>The result of the T operation.</returns>
    IReadOnlyList<T> MapList<T>(IDataReader reader);
}

public sealed class ReflectionForgeObjectMapper : IForgeObjectMapper
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="record">The record value.</param>
    /// <returns>The result of the T operation.</returns>
    public T Map<T>(IDataRecord record)
    {
        var obj = Activator.CreateInstance<T>();
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite).ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < record.FieldCount; i++)
        {
            if (!props.TryGetValue(record.GetName(i), out var prop)) continue;
            var value = record.IsDBNull(i) ? null : record.GetValue(i);
            if (value == null) { prop.SetValue(obj, null); continue; }
            var target = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            prop.SetValue(obj, Convert.ChangeType(value, target));
        }
        return obj;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="reader">The reader value.</param>
    /// <returns>The result of the T operation.</returns>
    public IReadOnlyList<T> MapList<T>(IDataReader reader)
    {
        var list = new List<T>();
        while (reader.Read()) list.Add(Map<T>(reader));
        return list;
    }
}
