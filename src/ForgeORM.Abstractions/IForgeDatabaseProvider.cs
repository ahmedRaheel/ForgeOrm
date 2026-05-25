using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

public interface IForgeDatabaseProvider
/// <summary>
/// Defines the CreateConnection operation.
/// </summary>
/// <param name="connectionString">The connectionString value.</param>
/// <returns>The result of the CreateConnection operation.</returns>
{
    /// <summary>
    /// Defines the CreateConnection operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <returns>The result of the CreateConnection operation.</returns>
    string ProviderName { get; }
    /// <summary>
    /// Defines the CreateConnection operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <returns>The result of the CreateConnection operation.</returns>
    ForgeSqlDialect Dialect { get; }
    /// <summary>
    /// Defines the CreateConnection operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <returns>The result of the CreateConnection operation.</returns>
    ForgeProviderCapabilities Capabilities { get; }

/// <summary>

/// Defines the CreateConnection operation.

/// </summary>

/// <param name="connectionString">The connectionString value.</param>

/// <returns>The result of the CreateConnection operation.</returns>

    /// <summary>
    /// Defines the CreateConnection operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <returns>The result of the CreateConnection operation.</returns>
    DbConnection CreateConnection(string connectionString);
    /// <summary>
    /// Defines the BuildGetById operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="id">The id value.</param>
    /// <returns>The result of the BuildGetById operation.</returns>
    ForgeCommand BuildGetById(ForgeEntityMetadata entity, object id);
    /// <summary>
    /// Defines the BuildGetByCode operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="code">The code value.</param>
    /// <returns>The result of the BuildGetByCode operation.</returns>
    ForgeCommand BuildGetByCode(ForgeEntityMetadata entity, string code);
    /// <summary>
    /// Defines the BuildGetByIds operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="ids">The ids value.</param>
    /// <returns>The result of the BuildGetByIds operation.</returns>
    ForgeCommand BuildGetByIds(ForgeEntityMetadata entity, IReadOnlyCollection<int> ids);
    /// <summary>
    /// Defines the BuildInsert operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="entityInstance">The entityInstance value.</param>
    /// <returns>The result of the BuildInsert operation.</returns>
    ForgeCommand BuildInsert(ForgeEntityMetadata entity, object entityInstance);
    /// <summary>
    /// Defines the BuildUpdate operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="entityInstance">The entityInstance value.</param>
    /// <returns>The result of the BuildUpdate operation.</returns>
    ForgeCommand BuildUpdate(ForgeEntityMetadata entity, object entityInstance);
    /// <summary>
    /// Defines the BuildDelete operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="id">The id value.</param>
    /// <returns>The result of the BuildDelete operation.</returns>
    ForgeCommand BuildDelete(ForgeEntityMetadata entity, object id);
    /// <summary>
    /// Defines the BuildPage operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <returns>The result of the BuildPage operation.</returns>
    ForgeCommand BuildPage(ForgePageRequest request);
    /// <summary>
    /// Defines the BuildCount operation.
    /// </summary>
    /// <param name="baseSql">The baseSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the BuildCount operation.</returns>
    ForgeCommand BuildCount(string baseSql, object? parameters = null);
    /// <summary>
    /// Defines the BuildBulkDelete operation.
    /// </summary>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="ids">The ids value.</param>
    /// <returns>The result of the BuildBulkDelete operation.</returns>
    ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids);
    /// <summary>
    /// Defines the BuildFunctionScalar operation.
    /// </summary>
    /// <param name="functionName">The functionName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the BuildFunctionScalar operation.</returns>
    ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    ValueTask BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    ValueTask BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    ValueTask BulkMergeAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default);
}
