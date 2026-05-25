using System.Linq.Expressions;
using System.Text;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>Starts a fluent db.Cte&lt;T&gt;() query.</summary>
    public ForgeDbCteQuery<T> Cte<T>() => new(this);

    /// <summary>Starts a fluent db.TempTable&lt;T&gt;("#Name") builder.</summary>
    public ForgeDbTempTableQuery<T> TempTable<T>(string name) => new(this, name);
}
