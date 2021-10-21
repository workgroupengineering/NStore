using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NStore.Core.Processing
{
    public static class FastMethodInvoker
    {
        static BindingFlags NonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        static BindingFlags Public = BindingFlags.Public | BindingFlags.Instance;

        public static object CallNonPublicIfExists(object instance, string methodName, object @parameter)
        {
            var cacheKey = $"{instance.GetType().FullName}/{methodName}";
            if (!lcgCacheFunction.TryGetValue(cacheKey, out var caller))
            {
                var mi = instance.GetType().GetMethod(
                    methodName,
                    NonPublic,
                    null,
                    new Type[] { @parameter.GetType() },
                    null
                );

                caller = ReflectFunction(instance.GetType(), mi);

                lcgCacheFunction[cacheKey] = caller;
            }
            return caller(instance, @parameter);
        }

        public static object CallNonPublicIfExists(object instance, string[] methodNames, object @parameter)
        {
            throw new NotImplementedException();
        }

        public static object CallPublic(object instance, string methodName, object @parameter)
        {
            var cacheKey = $"{instance.GetType().FullName}/{methodName}";
            if (!lcgCacheFunction.TryGetValue(cacheKey, out var caller))
            {
                var mi = instance.GetType().GetMethod(
                    methodName,
                    Public,
                    null,
                    new Type[] { @parameter.GetType() },
                    null
                );

                caller = ReflectFunction(instance.GetType(), mi);

                lcgCacheFunction[cacheKey] = caller;
            }
            return caller(instance, @parameter);
        }

        public static void CallPublicReturningVoid(object instance, string methodName, object @parameter)
        {
            var cacheKey = $"{instance.GetType().FullName}/{methodName}";
            if (!lcgCache.TryGetValue(cacheKey, out var caller))
            {
                var mi = instance.GetType().GetMethod(
                    methodName,
                    Public,
                    null,
                    new Type[] { @parameter.GetType() },
                    null
                );

                caller = ReflectAction(instance.GetType(), mi);

                lcgCache[cacheKey] = caller;
            }
            caller(instance, @parameter);
        }

        /// <summary>
        /// Cache of appliers, for each domain object I have a dictionary of actions
        /// </summary>
        private static ConcurrentDictionary<string, Action<Object, Object>> lcgCache = new ConcurrentDictionary<string, Action<object, object>>();
        private static ConcurrentDictionary<string, Func<Object, Object, Object>> lcgCacheFunction = new ConcurrentDictionary<string, Func<Object, Object, Object>>();

        private class ObjectApplier
        {
            private Dictionary<Type, RuntimeMethodHandle> appliers =
                new Dictionary<Type, RuntimeMethodHandle>();
        }

        public static Action<Object, Object> ReflectAction(Type objType, MethodInfo methodinfo)
        {
            DynamicMethod retmethod = new DynamicMethod(
                "Invoker" + methodinfo.Name,
                (Type)null,
                new Type[] { typeof(Object), typeof(Object) },
                objType,
                true); //methodinfo.GetParameters().Single().ParameterType
            ILGenerator ilgen = retmethod.GetILGenerator();
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Castclass, objType);
            ilgen.Emit(OpCodes.Ldarg_1);
            ilgen.Emit(OpCodes.Callvirt, methodinfo);
            ilgen.Emit(OpCodes.Ret);
            return (Action<Object, Object>)retmethod.CreateDelegate(typeof(Action<Object, Object>));
        }

        public static Func<Object, Object, Object> ReflectFunction(Type objType, MethodInfo methodinfo)
        {
            DynamicMethod retmethod = new DynamicMethod(
                "Invoker" + methodinfo.Name,
                typeof(Object),
                new Type[] { typeof(Object), typeof(Object) },
                objType,
                true); //methodinfo.GetParameters().Single().ParameterType
            ILGenerator ilgen = retmethod.GetILGenerator();
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Castclass, objType);
            ilgen.Emit(OpCodes.Ldarg_1);
            ilgen.Emit(OpCodes.Callvirt, methodinfo);
            ilgen.Emit(OpCodes.Ret);
            return (Func<Object, Object, Object>)retmethod.CreateDelegate(typeof(Func<Object, Object, Object>));
        }
    }
}