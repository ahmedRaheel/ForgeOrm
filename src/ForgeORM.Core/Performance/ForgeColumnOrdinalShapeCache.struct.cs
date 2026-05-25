using System.Collections.Concurrent;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

public readonly record struct DbDataReaderColumnShape(string Name, Type ClrType, string DbTypeName, bool AllowDBNull);
