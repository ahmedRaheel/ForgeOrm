using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Migration operation produced by schema diff.
/// </summary>
public sealed record ForgeMigrationOperation(
    string Operation,
    string Target,
    string Sql);
