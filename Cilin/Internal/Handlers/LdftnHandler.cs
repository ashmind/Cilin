using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.Reflection;
using Cilin.Internal.State;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilin.Internal.Handlers {
    public class LdftnHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Ldftn;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var method = context.Resolver.Method((MethodReference)instruction.Operand, context.GenericScope);
            var interpreted = method as InterpretedMethod;
            if (interpreted != null) {
                context.Stack.Push(new MethodPointerWrapper(interpreted));
                return;
            }

            context.Stack.Push(method.MethodHandle.GetFunctionPointer());
        }
    }
}
