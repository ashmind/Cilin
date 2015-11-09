using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Internal {
    public class BrHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Br;
            yield return OpCodes.Br_S;
            yield return OpCodes.Brfalse;
            yield return OpCodes.Brfalse_S;
            yield return OpCodes.Brtrue;
            yield return OpCodes.Brtrue_S;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            if (instruction.OpCode.FlowControl == FlowControl.Cond_Branch) {
                var condition = TypeSupport.Convert<bool>(context.Stack.Pop());
                var jumpExpectation = (instruction.OpCode == OpCodes.Brtrue || instruction.OpCode == OpCodes.Brtrue_S);

                if (condition != jumpExpectation)
                    return;
            }

            var target = (Instruction)instruction.Operand;
            context.NextInstruction = target;
        }
    }
}
