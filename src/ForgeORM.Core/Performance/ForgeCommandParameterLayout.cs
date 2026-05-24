using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Prepares the cached parameter layout on a fresh DbCommand before binding values.
/// This keeps binder delegates focused on assigning values and avoids repeated parameter-name decisions.
/// </summary>
internal static class ForgeCommandParameterLayout
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Prepare(DbCommand command, string[] parameterNames)
    {
        if (parameterNames.Length == 0) return;

        for (var i = 0; i < parameterNames.Length; i++)
        {
            var name = ForgeParameterBinderCompiler.NormalizeParameterName(parameterNames[i]);
            if (Contains(command, name)) continue;

            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            command.Parameters.Add(parameter);
        }
    }

    private static bool Contains(DbCommand command, string parameterName)
    {
        var normalized = parameterName.TrimStart('@', ':');
        for (var i = 0; i < command.Parameters.Count; i++)
        {
            if (command.Parameters[i] is not DbParameter p) continue;
            if (string.Equals(p.ParameterName.TrimStart('@', ':'), normalized, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
