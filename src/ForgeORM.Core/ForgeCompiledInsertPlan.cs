using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using ForgeORM.Abstractions;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

internal sealed class ForgeCompiledInsertPlan
{
    public required string Sql { get; init; }

    public required PropertyInfo[] Properties { get; init; }

    public required Func<object, object?>[] Getters { get; init; }
}
