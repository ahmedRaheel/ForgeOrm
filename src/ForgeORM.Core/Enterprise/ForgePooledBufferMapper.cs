using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal static class ForgePooledBufferMapper<TSource, TDestination>
    where TDestination : class
{
    public static readonly Func<TSource, TDestination> Map = Build();

    private static Func<TSource, TDestination> Build()
    {
        if (typeof(TDestination).IsAssignableFrom(typeof(TSource)))
            return source => (TDestination)(object?)source!;

        var destinationFactory = ForgeRuntimeAccessorCache.Constructor(typeof(TDestination));
        var sourceProps = typeof(TSource).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(p => p.CanRead).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        var destProps = typeof(TDestination).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(p => p.CanWrite).ToArray();

        return source =>
        {
            var destination = destinationFactory();
            foreach (var dest in destProps)
            {
                if (!sourceProps.TryGetValue(dest.Name, out var src)) continue;
                var value = ForgeRuntimeAccessorCache.Get(src, source!);
                ForgeRuntimeAccessorCache.Set(dest, destination, value);
            }
            return (TDestination)destination;
        };
    }
}
