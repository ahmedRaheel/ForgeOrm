using System.Collections.Concurrent;
using System.Reflection;

namespace ForgeORM.Core;

internal sealed record ForgeCompiledIncludePlan(
    Type RootType,
    IReadOnlyList<PropertyInfo> Includes,
    bool UseSplitQuery,
    bool UseIdentityResolution,
    string Fingerprint);
