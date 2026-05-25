using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// Selects how ForgeORM obtains compiled readers, binders and accessors.
/// Auto prefers source-generated code when registered and falls back to RuntimeEmit.
/// NativeAOT users can explicitly select SourceGenerated from configuration.
/// </summary>

/// <summary>
/// Strongly-typed parameter binder used by generated code and high-performance fallback binders.
/// This avoids boxing the parameter object and removes MethodInfo.Invoke from hot paths.
/// </summary>
public interface IForgeParameterBinder<T>
{
    void Bind(DbCommand command, T value);
}
