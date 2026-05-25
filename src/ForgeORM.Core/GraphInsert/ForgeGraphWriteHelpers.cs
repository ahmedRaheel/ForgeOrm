using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

internal static partial class ForgeGraphWriteHelpers
{
    internal static void ResetDatabaseGeneratedIdentity(object entity)
    {
        if (entity is null)
            return;

        var identity = entity.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));

        if (identity is null)
            return;

        if (!identity.CanWrite)
            return;

        var type = Nullable.GetUnderlyingType(identity.PropertyType)
                   ?? identity.PropertyType;

        object? defaultValue = ForgeRuntimeAccessorCache.DefaultValue(type);

        ForgeRuntimeAccessorCache.Set(identity, entity, defaultValue);
    }

    internal static void SetDatabaseGeneratedIdentity(
        object entity,
        object? generatedId)
    {
        if (entity is null)
            return;

        if (generatedId is null || generatedId == DBNull.Value)
            return;

        var identity = entity.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));

        if (identity is null)
            return;

        if (!identity.CanWrite)
            return;

        var targetType =
            Nullable.GetUnderlyingType(identity.PropertyType)
            ?? identity.PropertyType;

        var converted =
            Convert.ChangeType(generatedId, targetType);

        ForgeRuntimeAccessorCache.Set(identity, entity, converted);
    }
}
