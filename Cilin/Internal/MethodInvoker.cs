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
            if (!method.IsStatic) {
                if (target == null)
                    throw new NullReferenceException($"Attempted to call non-static method {method.Name} on null reference.");

                var custom = target as ICustomInvoker;
                if (custom != null)
                    return custom.Invoke(method, arguments, invokeAttr, binder, culture);
            }

            if (!method.IsStatic && !method.IsConstructor) {
                var targetType = TypeSupport.GetTypeOf(target);
                if (method.DeclaringType != targetType) {
                    binder = binder ?? Type.DefaultBinder;
                    var methods = targetType.GetMethods().Where(m => m.Name == method.Name).ToArray();

                    object state;
                    method = (InterpretedMethod)binder.BindToMethod(invokeAttr, methods, ref arguments, null, culture, null, out state);
                }
            }

            return _interpreter.InterpretCall(method.DeclaringType.GetGenericArguments(), definition, method.GetGenericArguments(), target, arguments);
        }
    }
}