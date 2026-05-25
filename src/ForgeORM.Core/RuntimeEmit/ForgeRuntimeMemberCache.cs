using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace ForgeORM.Core;

internal static class ForgeRuntimeMemberCache
{
    private static readonly ConcurrentDictionary<FieldInfo, Func<object, object?>> FieldGetters = new();
    private static readonly ConcurrentDictionary<FieldInfo, Action<object, object?>> FieldSetters = new();
    private static readonly ConcurrentDictionary<Type, Func<object, object?>> TaskResultGetters = new();

    public static object? Get(FieldInfo field, object instance) => FieldGetters.GetOrAdd(field, BuildFieldGetter)(instance);
    public static void Set(FieldInfo field, object instance, object? value) => FieldSetters.GetOrAdd(field, BuildFieldSetter)(instance, value);
    public static object? GetTaskResult(Task task) => TaskResultGetters.GetOrAdd(task.GetType(), BuildTaskResultGetter)(task);

    public static async ValueTask<object?> AwaitAndGetResultAsync(object awaitable)
    {
        ArgumentNullException.ThrowIfNull(awaitable);

        if (awaitable is Task task)
        {
            await task.ConfigureAwait(false);
            return task.GetType().IsGenericType ? GetTaskResult(task) : null;
        }

        if (awaitable is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);
            return null;
        }

        var type = awaitable.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var asTask = type.GetMethod(nameof(ValueTask<int>.AsTask), BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"ValueTask type {type.FullName} does not expose AsTask().");
            var taskObj = (Task)asTask.Invoke(awaitable, null)!;
            await taskObj.ConfigureAwait(false);
            return GetTaskResult(taskObj);
        }

        throw new InvalidOperationException($"Unsupported async return type: {type.FullName}");
    }

    private static Func<object, object?> BuildFieldGetter(FieldInfo field)
    {
        var method = new DynamicMethod($"ForgeORM_FieldGet_{field.Name}", typeof(object), new[] { typeof(object) }, typeof(ForgeRuntimeMemberCache).Module, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(field.DeclaringType!.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, field.DeclaringType!);
        il.Emit(OpCodes.Ldfld, field);
        if (field.FieldType.IsValueType) il.Emit(OpCodes.Box, field.FieldType);
        il.Emit(OpCodes.Ret);
        return (Func<object, object?>)method.CreateDelegate(typeof(Func<object, object?>));
    }

    private static Action<object, object?> BuildFieldSetter(FieldInfo field)
    {
        var method = new DynamicMethod($"ForgeORM_FieldSet_{field.Name}", typeof(void), new[] { typeof(object), typeof(object) }, typeof(ForgeRuntimeMemberCache).Module, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(field.DeclaringType!.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, field.DeclaringType!);
        il.Emit(OpCodes.Ldarg_1);
        if (field.FieldType.IsValueType) il.Emit(OpCodes.Unbox_Any, field.FieldType); else il.Emit(OpCodes.Castclass, field.FieldType);
        il.Emit(OpCodes.Stfld, field);
        il.Emit(OpCodes.Ret);
        return (Action<object, object?>)method.CreateDelegate(typeof(Action<object, object?>));
    }

    private static Func<object, object?> BuildTaskResultGetter(Type taskType)
    {
        var result = taskType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Task type {taskType.FullName} does not expose Result.");
        return ForgeRuntimeAccessorCache.Getter(result);
    }
}
