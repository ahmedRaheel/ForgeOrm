using System;
using ForgeORM.Abstractions;
using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

public interface IForgeAstTempTableBuilder
/// <summary>
/// Defines the Column operation.
/// </summary>
/// <param name="name">The name value.</param>
/// <param name="dbType">The dbType value.</param>
/// <param name="nullable">The nullable value.</param>
/// <returns>The result of the Column operation.</returns>
{
    /// <summary>
    /// Defines the Column operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="dbType">The dbType value.</param>
    /// <param name="nullable">The nullable value.</param>
    /// <returns>The result of the Column operation.</returns>
    IForgeAstTempTableBuilder Column(string name, string dbType, bool nullable = true);
    /// <summary>
    /// Defines the PrimaryKey operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the PrimaryKey operation.</returns>
    IForgeAstTempTableBuilder PrimaryKey(params string[] columns);
    /// <summary>
    /// Defines the Build operation.
    /// </summary>
    /// <returns>The result of the Build operation.</returns>
    ForgeTempTable Build();
}
