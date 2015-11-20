using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilin.Internal {
    public class CallAndNewObjHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Newobj;
            yield return OpCodes.Callvirt;
            yield return OpCodes.Call;
        }
        
        public void Handle(Instruction instruction, CilHandlerContext context) {
            var method = context.Resolver.Method((MethodReference)instruction.Operand, context.GenericScope);

            var parameters = method.GetParameters();
            var arguments = new object[parameters.Length];
            for (var i = parameters.Length - 1; i >= 0; i--) {
                arguments[i] = TypeSupport.Convert(context.Stack.Pop(), parameters[i].ParameterType);
            }

            if (instruction.OpCode == OpCodes.Newobj) {
                var instance = context.Invoker.Invoke(method, null, arguments);
                context.Stack.Push(instance);
                return;
            }

            var target = (object)null;
            if (!method.IsStatic)
                target = TypeSupport.Convert(context.Stack.Pop(), method.DeclaringType);

            var result = context.Invoker.Invoke(method, target, arguments);
            if (!method.IsConstructor && ((MethodInfo)method).ReturnType != typeof(void))
                context.Stack.Push(result);
        }
    }
}
