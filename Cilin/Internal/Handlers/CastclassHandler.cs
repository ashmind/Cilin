using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.State;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilin.Internal.Handlers {
    public class CastclassHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Castclass;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var type = context.Resolver.Type((TypeReference)instruction.Operand, context.GenericScope);
            var instance = context.Stack.Pop();
            if (!type.IsInstanceOfType(instance))
                throw new InvalidCastException($"Specified cast is not valid (instance: {instance}, type: {type}).");

            context.Stack.Push(instance);
        }
    }
}
