using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Internal {
    public class AddHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Add;
            yield return OpCodes.Add_Ovf;
            yield return OpCodes.Add_Ovf_Un;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var left = context.Stack.Pop();
            var right = context.Stack.Pop();

            var result = instruction.OpCode == OpCodes.Add
                       ? (dynamic)left + (dynamic)right
                       : checked((dynamic)left + (dynamic)right);

            context.Stack.Push(result);
        }
    }
}
