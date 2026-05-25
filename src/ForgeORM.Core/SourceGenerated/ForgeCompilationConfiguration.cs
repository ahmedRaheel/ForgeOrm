namespace ForgeORM.Core;

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
