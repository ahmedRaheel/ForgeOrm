using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

public interface IForgeDiagnostics
/// <summary>
/// Defines the Analyze operation.
/// </summary>
/// <param name="sql">The sql value.</param>
/// <returns>The result of the Analyze operation.</returns>
{
    /// <summary>
    /// Defines the Analyze operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the Analyze operation.</returns>
    ForgeQueryAnalysis Analyze(string sql);
}
