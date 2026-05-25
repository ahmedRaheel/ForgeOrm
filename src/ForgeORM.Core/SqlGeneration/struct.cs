using System.Collections.Concurrent;
using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public readonly record struct ForgeGeneratedSqlKey(Type EntityType, string ProviderName);
