using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ForgeORM.Abstractions;

namespace ForgeORM.Core.Performance;

internal static class ForgeRawEnumParameterMap<T>
{
    internal static readonly System.Collections.Generic.IReadOnlyDictionary<string, Type> Map = Build();

    private static System.Collections.Generic.IReadOnlyDictionary<string, Type> Build()
    {
        var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (properties.Length == 0)
            return new System.Collections.Generic.Dictionary<string, Type>(0, StringComparer.OrdinalIgnoreCase);

        var result = new System.Collections.Generic.Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (!type.IsEnum)
                continue;

            result[property.Name] = type;

            // Also support [ForgeColumn("StatusId")] / [Column("StatusId")] style attributes without
            // referencing those attribute types directly from this hot-path helper.
            foreach (var attribute in property.GetCustomAttributes(inherit: true))
            {
                var attrType = attribute.GetType();
                var nameProperty = attrType.GetProperty("Name") ?? attrType.GetProperty("ColumnName");
                if (nameProperty?.GetValue(attribute) is string columnName && !string.IsNullOrWhiteSpace(columnName))
                    result[columnName] = type;
            }
        }

        return result.Count == 0
            ? new System.Collections.Generic.Dictionary<string, Type>(0, StringComparer.OrdinalIgnoreCase)
            : result;
    }
}
