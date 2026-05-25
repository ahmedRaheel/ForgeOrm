namespace ForgeORM.Core;

/// <summary>
/// Runtime configuration for ForgeORM compilation strategy. NativeAOT applications can explicitly choose
/// SourceGenerated; regular JIT applications can choose Auto or RuntimeEmit.
/// </summary>
public sealed class ForgeCompilationOptions
{
    public ForgeOrmCompilationMode Mode { get; set; } = ForgeOrmCompilationMode.Auto;
}

public static class ForgeCompilationConfiguration
{
    public static void ConfigureCompilation(Action<ForgeCompilationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new ForgeCompilationOptions();
        configure(options);
        ForgeSourceGeneratedRegistry.CompilationMode = options.Mode;
    }
}
