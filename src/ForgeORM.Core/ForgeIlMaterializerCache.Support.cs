using ForgeORM.Abstractions;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;

namespace ForgeORM.Core;

internal static class Support
{
    public static bool IsScalarColumn(PropertyInfo property)
    {
        return IsScalarColumnType(property.PropertyType);
    }

    public static bool IsScalarColumnType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(Guid)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            || type == typeof(byte[]);
    }

    public static bool IsCollectionNavigation(PropertyInfo property)
    {
        if (property.PropertyType == typeof(string))
            return false;

        return property.PropertyType.IsGenericType
            && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>);
    }

    public static bool IsReferenceNavigation(PropertyInfo property)
    {
        if (property.PropertyType == typeof(string))
            return false;

        if (IsCollectionNavigation(property))
            return false;

        var type = Nullable.GetUnderlyingType(property.PropertyType)
                   ?? property.PropertyType;

        return type.IsClass && !IsScalarColumnType(type);
    }

    public static string ResolveTableName(Type type)
    {
        return type.GetCustomAttribute<ForgeTableAttribute>()?.Name
            ?? type.Name;
    }

    public static string ResolveScalarColumns(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columns = new List<string>(properties.Length);

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            if (!IsScalarColumn(property))
                continue;

            var column = property.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? property.Name;
            var exists = false;
            for (var c = 0; c < columns.Count; c++)
            {
                if (!string.Equals(columns[c], column, StringComparison.OrdinalIgnoreCase)) continue;
                exists = true;
                break;
            }

            if (!exists)
                columns.Add(column);
        }

        return columns.Count == 0 ? "*" : string.Join(", ", columns);
    }

    public static PropertyInfo[] GetScalarProperties(
        Type type,
        bool includeIdentity = false)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var result = new List<PropertyInfo>(properties.Length);

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            if (!property.CanRead) continue;
            if (!IsScalarColumn(property)) continue;
            if (!includeIdentity && IsIdentityColumn(property)) continue;
            result.Add(property);
        }

        return result.Count == 0 ? Array.Empty<PropertyInfo>() : result.ToArray();
    }

    public static bool IsIdentityColumn(PropertyInfo property)
    {
        if (property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            return true;

        var attributes = property.GetCustomAttributes();
        for (var i = 0; i < attributes.Length; i++)
        {
            if (attributes[i].GetType().Name.Contains("Key", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static object? NormalizeParameterValue(object? value)
    {
        if (value is null)
            return null;

        if (value is Enum enumValue)
        {
            return Convert.ChangeType(
                enumValue,
                Enum.GetUnderlyingType(enumValue.GetType()));
        }

        if (value is DateTime dateTime)
        {
            if (dateTime == default ||
                dateTime < new DateTime(1753, 1, 1))
            {
                return DateTime.UtcNow;
            }

            return dateTime;
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            if (dateTimeOffset == default)
                return DateTimeOffset.UtcNow;

            return dateTimeOffset;
        }

        return value;
    }

    public static (int Skip, int Take) NormalizePaging(
        int skip,
        int take)
    {
        if (skip < 0)
            skip = 0;

        if (take <= 0)
            take = 1;

        if (skip == take)
            take++;

        return (skip, take);
    }

    public static void ResetIdentityValue(
        object entity,
        PropertyInfo? identityProperty)
    {
        if (identityProperty is null)
            return;

        var type = Nullable.GetUnderlyingType(identityProperty.PropertyType)
                   ?? identityProperty.PropertyType;

        object? value = ForgeRuntimeAccessorCache.DefaultValue(type);

        ForgeRuntimeAccessorCache.Set(identityProperty, entity, value);
    }
}
