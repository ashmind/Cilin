using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.Reflection;
using Cilin.Internal.State;
using Mono.Cecil;

namespace Cilin.Internal {
    public class MethodInvoker {
        private readonly Interpreter _interpreter;

        public MethodInvoker(Interpreter interpreter) {
            _interpreter = interpreter;
        }

        public object Invoke(MethodBase method, object target, object[] arguments, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
            var interpreted = method as IInterpretedMethodBase;
            if (interpreted == null) {
                if (!(method.IsStatic && method.IsConstructor))
                    interpreted.DeclaringType.EnsureStaticConstructorRun();
                return method.Invoke(target, invokeAttr, binder, arguments, culture);
            }

            return InvokeInterpreted(method, (MethodDefinition)interpreted.InvokableDefinition, target, arguments, invokeAttr, binder, culture);
        }

        private object InvokeInterpreted(MethodBase method, MethodDefinition definition, object target, object[] arguments, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
            if (binder != null)
                throw new NotSupportedException();

            if (method.DeclaringType.ContainsGenericParameters)
                throw new Exception($"Attempted to call method {method} on open generic type {method.DeclaringType}.");

            if ((method as MethodInfo)?.ContainsGenericParameters ?? false)
                throw new Exception($"Attempted to call open generic method {method}.");

            if (!method.IsStatic) {
                if (target == null)
                    throw new NullReferenceException($"Attempted to call non-static method {method.Name} on null reference.");

                var custom = target as ICustomInvoker;
                if (custom != null)
                    return custom.Invoke(method, arguments, invokeAttr, binder, culture);
            }

            if (!method.IsStatic && !method.IsConstructor) {
                var targetType = TypeSupport.GetTypeOf(target);
                if (method.DeclaringType != targetType)
                    method = GetMatchingMethod(targetType, (MethodInfo)method);
            }

            return _interpreter.InterpretCall(method.DeclaringType.GetGenericArguments(), definition, method.GetGenericArguments(), target, arguments);
        }

        private MethodInfo GetMatchingMethod(Type targetType, MethodInfo method) {
            if (!method.IsVirtual && !method.DeclaringType.IsInterface)
                return method;

            if (method.DeclaringType.IsInterface)
                throw new NotImplementedException();

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