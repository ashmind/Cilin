using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilin.Core.Internal.Handlers {
    public class IsinstHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Isinst;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var type = context.Resolver.Type((TypeReference)instruction.Operand, context.GenericScope);
            var instance = context.Stack.Pop();
            context.Stack.Push(type.IsInstanceOfType(instance) ? instance : null);
        }
    }
}
