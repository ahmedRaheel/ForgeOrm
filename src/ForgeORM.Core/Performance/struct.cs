using System.Collections.Concurrent;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

public readonly record struct ForgeColumnShapeKey(string ProviderName, Type TargetType, string ShapeFingerprint);
