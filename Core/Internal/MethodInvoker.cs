using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Cilin.Core.Internal.Reflection;
using Cilin.Core.Internal.State;

namespace Cilin.Core.Internal {
    public class MethodInvoker {
        private static readonly ConstructorInfo ObjectConstructor = typeof(object).GetConstructors().Single();
        private static readonly MethodInfo TypeGetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));
        private static readonly MethodInfo AssemblyGetExecutingAssembly = typeof(Assembly).GetMethod(nameof(Assembly.GetExecutingAssembly));

        private readonly Interpreter _interpreter;

        public MethodInvoker(Interpreter interpreter) {
            _interpreter = interpreter;
        }

        public object Invoke(MethodBase method, object target, object[] arguments, MethodBase caller, BindingFlags invokeAttr = BindingFlags.Default, Binder binder = null, CultureInfo culture = null) {
            if (binder != null)
                throw new NotSupportedException();

            if (method.DeclaringType.ContainsGenericParameters)
                throw new Exception($"Attempted to call method {method} on open generic type {method.DeclaringType}.");

            if ((method as MethodInfo)?.ContainsGenericParameters ?? false)
                throw new Exception($"Attempted to call open generic method {method}.");

            if (method.IsConstructor && !method.IsStatic && (target == null))
                return InvokeConstructorForNewObject((ConstructorInfo)method, arguments, caller);

            object result;
            if (TryInvokeSpecialMethod(method, arguments, caller, out result))
                return result;

            if (!method.IsStatic) {
                if (target == null)
                    throw new NullReferenceException($"Attempted to call instance method {method.Name} on null reference.");

                var custom = target as ICustomInvoker;
                if (custom != null)
                    return custom.Invoke(method, arguments, invokeAttr, binder, culture);
            }
            
            if (!(method is IInterpretedMethodBase)) {
                var unwrapped = ObjectWrapper.UnwrapIfRequired(target);
                if (!(unwrapped is INonRuntimeObject)) {
                    if (method.DeclaringType.IsInterface && !unwrapped.GetType().IsArray)
                        method = GetMatchingMethod(unwrapped.GetType(), (MethodInfo)method);

                    return method.Invoke(unwrapped, invokeAttr, binder, arguments, culture);
                }
            }

            if (!method.IsStatic && !method.IsConstructor) {
                var targetType = TypeSupport.GetTypeOf(target);
                if (method.DeclaringType != targetType)
                    method = GetMatchingMethod(targetType, (MethodInfo)method);
            }

            var interpreted = method as IInterpretedMethodBase;
            if (interpreted == null)
                throw new Exception($"Cannot invoke runtime method {method} on non-runtime instance {target}.");

            return _interpreter.InterpretCall(
                method.DeclaringType.GetGenericArguments(),
                (MethodBase)interpreted, (MethodDefinition)interpreted.InvokableDefinition,
                method.GetGenericArguments(),
                target, arguments
            );
        }

        private object InvokeConstructorForNewObject(ConstructorInfo constructor, object[] arguments, MethodBase caller) {
            object instance;
            if (TryInvokeSpecialConstructor(constructor, arguments, out instance))
                return instance;

            if (!(constructor is IInterpretedMethodBase))
                return constructor.Invoke(arguments);

            instance = new CilinObject((InterpretedType)constructor.DeclaringType);
            Invoke(constructor, instance, arguments, caller);
            return instance;
        }

        private bool TryInvokeSpecialConstructor(ConstructorInfo constructor, object[] arguments, out object instance) {
            if (TypeSupport.IsDelegate(constructor.DeclaringType)) {
                instance = new CilinDelegate(arguments[0], (NonRuntimeMethodPointer)arguments[1], constructor.DeclaringType);
                return true;
            }

            instance = null;
            return false;
        }

        private bool TryInvokeSpecialMethod(MethodBase method, object[] arguments, MethodBase caller, out object result) {
            if (method == ObjectConstructor) {
                result = null;
                return true;
            }

            if (method == AssemblyGetExecutingAssembly) {
                result = caller.Module.Assembly;
                return true;
            }

            if (method == TypeGetTypeFromHandle) {
                var nonRuntime = arguments[0] as NonRuntimeHandle;
                if (nonRuntime != null) {
                    result = (Type)nonRuntime.Member;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private MethodInfo GetMatchingMethod(Type targetType, MethodInfo method) {
            if (!method.IsVirtual && !method.DeclaringType.IsInterface)
                return method;

            if (method.DeclaringType.IsInterface) {
                var map = targetType.GetInterfaceMap(method.DeclaringType);
                var index = Array.IndexOf(map.InterfaceMethods, method);
                if (index < 0)
                    throw new MissingMethodException($"Failed to find implementation of {method} on type {targetType}.");

                return map.TargetMethods[index];
            }

            var @base = method.GetBaseDefinition();
            var candidates = targetType.FindMembers(MemberTypes.Method, BindingFlags.Default, Type.FilterName, method.Name);
            foreach (MethodInfo candidate in candidates) {
                if (candidate.GetBaseDefinition() == @base)
                    return candidate;
            }

            throw new MissingMethodException($"Failed to find {method} on type {targetType}.");
        }
    }
}