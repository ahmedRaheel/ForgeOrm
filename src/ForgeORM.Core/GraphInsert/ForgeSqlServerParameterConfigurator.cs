using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

internal static class ForgeSqlServerParameterConfigurator
{
    /// <summary>
    /// Executes the ConfigureStructured operation.
    /// </summary>
    /// <param name="parameter">The parameter value.</param>
    /// <param name="tableType">The tableType value.</param>
    public static void ConfigureStructured(DbParameter parameter, string tableType)
    {
        if (parameter is SqlParameter sqlParameter)
        {
            sqlParameter.TypeName = tableType;
            sqlParameter.SqlDbType = SqlDbType.Structured;
            return;
        }

        throw new NotSupportedException("Structured TVP parameters require Microsoft.Data.SqlClient.SqlParameter.");
    }
}
