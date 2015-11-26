using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Internal {
    public class LeaveHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Leave;
            yield return OpCodes.Leave_S;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var target = (Instruction)instruction.Operand;
            context.NextInstruction = target;
        }
    }
}
