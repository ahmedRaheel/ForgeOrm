using System.Data;
using System.Reflection;

namespace ForgeORM.Mapping;

public interface IForgeObjectMapper
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="record">The record value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="record">The record value.</param>
    /// <returns>The result of the T operation.</returns>
    T Map<T>(IDataRecord record);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="reader">The reader value.</param>
    /// <returns>The result of the T operation.</returns>
    IReadOnlyList<T> MapList<T>(IDataReader reader);
}
