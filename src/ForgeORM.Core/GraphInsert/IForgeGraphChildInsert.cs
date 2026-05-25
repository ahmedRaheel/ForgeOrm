using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

internal interface IForgeGraphChildInsert<TDto>
{
    /// <summary>
    /// Defines the InsertAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="dto">The dto value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the InsertAsync operation.</returns>
    ValueTask InsertAsync(DbConnection connection, DbTransaction transaction, TDto dto, object parentKey, CancellationToken cancellationToken);
}
