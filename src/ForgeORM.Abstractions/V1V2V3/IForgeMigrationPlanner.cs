namespace ForgeORM.Abstractions;

public interface IForgeMigrationPlanner
/// <summary>
/// Defines the Plan operation.
/// </summary>
/// <param name="name">The name value.</param>
/// <param name="currentSchema">The currentSchema value.</param>
/// <param name="targetSchema">The targetSchema value.</param>
/// <returns>The result of the Plan operation.</returns>
{
    /// <summary>
    /// Defines the Plan operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="currentSchema">The currentSchema value.</param>
    /// <param name="targetSchema">The targetSchema value.</param>
    /// <returns>The result of the Plan operation.</returns>
    ForgeMigrationPlan Plan(string name, IReadOnlyList<string> currentSchema, IReadOnlyList<string> targetSchema);
}
