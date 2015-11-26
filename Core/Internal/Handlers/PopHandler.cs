using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Core.Internal.Handlers {
    public class PopHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Pop;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            context.Stack.Pop();
        }
    }
}
