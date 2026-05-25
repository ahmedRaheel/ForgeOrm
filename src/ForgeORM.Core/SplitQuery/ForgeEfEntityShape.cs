using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;

namespace ForgeORM.Core;

internal sealed record ForgeEfEntityShape(
    Type EntityType,
    string TableName,
    IReadOnlyList<PropertyInfo> KeyProperties,
    IReadOnlyList<PropertyInfo> ScalarProperties,
    string ColumnList);
