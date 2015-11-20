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
        private static readonly ConstructorInfo ObjectConstructor = typeof(object).GetConstructors().Single();
        private readonly Interpreter _interpreter;

        public MethodInvoker(Interpreter interpreter) {
            _interpreter = interpreter;
        }

        public object Invoke(MethodBase method, object target, object[] arguments, BindingFlags invokeAttr = BindingFlags.Default, Binder binder = null, CultureInfo culture = null) {
            if (binder != null)
                throw new NotSupportedException();

            if (method.DeclaringType.ContainsGenericParameters)
                throw new Exception($"Attempted to call method {method} on open generic type {method.DeclaringType}.");

            if ((method as MethodInfo)?.ContainsGenericParameters ?? false)
                throw new Exception($"Attempted to call open generic method {method}.");

            if (method.IsConstructor && !method.IsStatic && (target == null))
                return InvokeConstructorForNewObject((ConstructorInfo)method, arguments);

            if (method == ObjectConstructor)
                return null;

            if (!method.IsStatic) {
                if (target == null)
                    throw new NullReferenceException($"Attempted to call non-static method {method.Name} on null reference.");

                var custom = target as ICustomInvoker;
                if (custom != null)
                    return custom.Invoke(method, arguments, invokeAttr, binder, culture);
            }

            if (!(method is IInterpretedMethodBase) && !(target is INonRuntimeObject))
                return method.Invoke(target, invokeAttr, binder, arguments, culture);

            if (!method.IsStatic && !method.IsConstructor) {
                var targetType = TypeSupport.GetTypeOf(target);
                if (method.DeclaringType != targetType)
                    method = GetMatchingMethod(targetType, (MethodInfo)method);
            }

            var interpreted = method as IInterpretedMethodBase;
            if (interpreted == null)
                throw new Exception($"Cannot invoke runtime method {method} on non-runtime instance {target}.");

            return _interpreter.InterpretCall(method.DeclaringType.GetGenericArguments(), (MethodDefinition)interpreted.InvokableDefinition, method.GetGenericArguments(), target, arguments);
        }

        private object InvokeConstructorForNewObject(ConstructorInfo constructor, object[] arguments) {
            object instance;
            if (TryInvokeSpecialConstructor(constructor, arguments, out instance))
                return instance;

            if (!(constructor is IInterpretedMethodBase))
                return constructor.Invoke(arguments);

            instance = new CilinObject((InterpretedType)constructor.DeclaringType);
            Invoke(constructor, instance, arguments);
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