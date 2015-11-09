using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Internal {
    public class StlocHandler : ICilHandler {
        private readonly IReadOnlyDictionary<OpCode, int> SpecialMap = new Dictionary<OpCode, int> {
            { OpCodes.Stloc_0, 0 },
            { OpCodes.Stloc_1, 1 },
            { OpCodes.Stloc_2, 2 },
            { OpCodes.Stloc_3, 3 }
        };

        public IEnumerable<OpCode> GetOpCodes() {
            foreach (var opCode in SpecialMap.Keys) {
                yield return opCode;
            }
            yield return OpCodes.Stloc;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var value = context.Stack.Pop();

            int index;
            if (!SpecialMap.TryGetValue(instruction.OpCode, out index))
                index = (int)instruction.Operand;

            context.SetVariable(index, value);
        }
    }
}
