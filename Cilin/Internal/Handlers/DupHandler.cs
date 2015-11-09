using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Internal {
    public class DupHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Dup;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            context.Stack.Push(context.Stack.Peek());
        }
    }
}
