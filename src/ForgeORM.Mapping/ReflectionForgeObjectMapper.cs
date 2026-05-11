using System.Data;
using System.Reflection;

namespace ForgeORM.Mapping;

public interface IForgeObjectMapper
{
    T Map<T>(IDataRecord record);
    IReadOnlyList<T> MapList<T>(IDataReader reader);
}

public sealed class ReflectionForgeObjectMapper : IForgeObjectMapper
{
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

    public IReadOnlyList<T> MapList<T>(IDataReader reader)
    {
        var list = new List<T>();
        while (reader.Read()) list.Add(Map<T>(reader));
        return list;
    }
}
