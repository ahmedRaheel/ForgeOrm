using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

internal static class ForgeEnumConversion
{
    /// <summary>
    /// Executes the StorageType operation.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <returns>The result of the StorageType operation.</returns>
    public static Type StorageType(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (!type.IsEnum) return type;

        var attr = property.GetCustomAttribute<ForgeEnumStorageAttribute>();
        return attr?.Storage == ForgeEnumStorage.Number
            ? Enum.GetUnderlyingType(type)
            : typeof(string);
    }

    /// <summary>
    /// Executes the ToDatabaseValue operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="property">The property value.</param>
    /// <returns>The result of the ToDatabaseValue operation.</returns>
    public static object? ToDatabaseValue(object? value, PropertyInfo? property = null)
    {
        if (value is null || value is DBNull) return value;

        var type = value.GetType();
        var enumType = Nullable.GetUnderlyingType(type) ?? type;
        if (!enumType.IsEnum) return value;

        var storage = property?.GetCustomAttribute<ForgeEnumStorageAttribute>()?.Storage ?? ForgeEnumStorage.String;
        return storage == ForgeEnumStorage.Number
            ? Convert.ChangeType(value, Enum.GetUnderlyingType(enumType))
            : value.ToString();
    }

    /// <summary>
    /// Executes the ToEnumOrValue operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="targetType">The targetType value.</param>
    /// <returns>The result of the ToEnumOrValue operation.</returns>
    public static object? ToEnumOrValue(object? value, Type targetType)
    {
        if (value is null || value is DBNull)
        {
            var nullable = Nullable.GetUnderlyingType(targetType);
            if (nullable is not null || !targetType.IsValueType) return null;
            return ForgeRuntimeAccessorCache.DefaultValue(targetType);
        }

        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (!type.IsEnum) return null;
        if (value is string text) return Enum.Parse(type, text, ignoreCase: true);
        return Enum.ToObject(type, value);
    }
}
