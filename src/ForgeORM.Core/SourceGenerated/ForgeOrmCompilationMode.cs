using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public enum ForgeOrmCompilationMode
{
    /// <summary>Prefer source-generated artifacts, but safely fall back to provider-native/MSIL runtime emit when a generated artifact is missing.</summary>
    Auto = 0,

    /// <summary>Force runtime emit/materializer fallback and ignore source-generated providers.</summary>
    RuntimeEmit = 1,

    /// <summary>Prefer source-generated artifacts globally, but do not break execution when a projection/entity was not generated.</summary>
    SourceGenerated = 2,

    /// <summary>NativeAOT/strict mode. Fail fast if any required generated artifact is missing.</summary>
    SourceGeneratedStrict = 3
}
