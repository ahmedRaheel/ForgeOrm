
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ForgeORM.NextGen;

public interface IForgeSemanticSearch
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="propertyOrColumn">The propertyOrColumn value.</param>
/// <param name="searchText">The searchText value.</param>
/// <param name="top">The top value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="propertyOrColumn">The propertyOrColumn value.</param>
    /// <param name="searchText">The searchText value.</param>
    /// <param name="top">The top value.</param>
    /// <returns>The result of the T operation.</returns>
    ForgeBuiltQuery SearchSemantic<T>(string propertyOrColumn, string searchText, int top = 20);
}
