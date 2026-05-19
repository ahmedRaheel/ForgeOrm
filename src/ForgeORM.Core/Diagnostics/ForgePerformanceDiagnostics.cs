namespace ForgeORM.Core;

/// <summary>
/// Low-allocation diagnostic hooks. The message factory is invoked only when the caller says diagnostics are enabled.
/// This keeps ForgeORM.Core free from a logging package dependency.
/// </summary>
public static class ForgePerformanceDiagnostics
{
    public static void WriteIfEnabled(bool enabled, Action<string>? sink, Func<string> messageFactory)
    {
        if (!enabled || sink is null)
            return;

        sink(messageFactory());
    }
}
