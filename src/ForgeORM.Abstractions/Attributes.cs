namespace ForgeORM.Abstractions;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ForgeTableAttribute : Attribute
{
    /// <summary>
    /// Executes the ForgeTableAttribute operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ForgeTableAttribute operation.</returns>
    public string Name { get; }
    /// <summary>
    /// Executes the ForgeTableAttribute operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ForgeTableAttribute operation.</returns>
    public ForgeTableAttribute(string name) => Name = name;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ForgeKeyAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public sealed class ForgeCodeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public sealed class ForgeColumnAttribute : Attribute
{
    /// <summary>
    /// Executes the ForgeColumnAttribute operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ForgeColumnAttribute operation.</returns>
    public string Name { get; }
    /// <summary>
    /// Executes the ForgeColumnAttribute operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ForgeColumnAttribute operation.</returns>
    public ForgeColumnAttribute(string name) => Name = name;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ForgeComputedAttribute : Attribute { }


public enum ForgeEnumStorage
{
    String = 0,
    Number = 1
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ForgeEnumStorageAttribute : Attribute
{
    /// <summary>
    /// Executes the ForgeEnumStorageAttribute operation.
    /// </summary>
    /// <param name="storage">The storage value.</param>
    /// <returns>The result of the ForgeEnumStorageAttribute operation.</returns>
    public ForgeEnumStorage Storage { get; }
    /// <summary>
    /// Executes the ForgeEnumStorageAttribute operation.
    /// </summary>
    /// <param name="storage">The storage value.</param>
    /// <returns>The result of the ForgeEnumStorageAttribute operation.</returns>
    public ForgeEnumStorageAttribute(ForgeEnumStorage storage) => Storage = storage;
}


[AttributeUsage(AttributeTargets.Class)]
public sealed class ForgeTemporalAttribute : Attribute
{
    public string? HistoryTable { get; }
    public string PeriodStartColumn { get; }
    public string PeriodEndColumn { get; }

    public ForgeTemporalAttribute(
        string? historyTable = null,
        string periodStartColumn = "ValidFrom",
        string periodEndColumn = "ValidTo")
    {
        HistoryTable = historyTable;
        PeriodStartColumn = periodStartColumn;
        PeriodEndColumn = periodEndColumn;
    }
}

public enum ForgeTemporalMode
{
    None = 0,
    All = 1,
    AsOf = 2,
    Between = 3,
    FromTo = 4,
    ContainedIn = 5
}
