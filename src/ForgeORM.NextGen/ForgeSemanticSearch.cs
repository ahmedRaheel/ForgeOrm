
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ForgeORM.NextGen;

public sealed class ForgeSemanticSearch : IForgeSemanticSearch
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="propertyOrColumn">The propertyOrColumn value.</param>
    /// <param name="searchText">The searchText value.</param>
    /// <param name="top">The top value.</param>
    /// <returns>The result of the T operation.</returns>
    public ForgeBuiltQuery SearchSemantic<T>(string propertyOrColumn, string searchText, int top = 20)
    {
        // Provider-specific implementations can replace this with pgvector, SQL Server vector search,
        // Azure AI Search, or custom embedding functions.
        var table = typeof(T).Name;
        var sql = $"SELECT TOP ({top}) * FROM {table} WHERE {propertyOrColumn} LIKE @SearchText";
        return new ForgeBuiltQuery
        {
            Sql = sql,
            Parameters = new { SearchText = "%" + searchText + "%" }
        };
    }
}
