using System.Linq.Expressions;
using System.Threading.Tasks;
using ForgeORM.Core;

namespace ForgeORM.DataFrame;

public enum ForgeVectorOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith
}
