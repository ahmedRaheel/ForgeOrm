using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryAst;

internal sealed class ForgeAstTempTableBuilder : IForgeAstTempTableBuilder
{
    private readonly ForgeTempTable _table;

    /// <summary>
    /// Executes the ForgeAstTempTableBuilder operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ForgeAstTempTableBuilder operation.</returns>
    public ForgeAstTempTableBuilder(string name)
    {
        _table = new ForgeTempTable { Name = name };
    }

    /// <summary>
    /// Executes the Column operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="dbType">The dbType value.</param>
    /// <param name="nullable">The nullable value.</param>
    /// <returns>The result of the Column operation.</returns>
    public IForgeAstTempTableBuilder Column(string name, string dbType, bool nullable = true)
    {
        _table.Columns.Add(new ForgeTempColumn(name, dbType, nullable));
        return this;
    }

    /// <summary>
    /// Executes the PrimaryKey operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the PrimaryKey operation.</returns>
    public IForgeAstTempTableBuilder PrimaryKey(params string[] columns)
    {
        _table.PrimaryKeyColumns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Executes the Build operation.
    /// </summary>
    /// <returns>The result of the Build operation.</returns>
    public ForgeTempTable Build() => _table;
}
