using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Core.Internal {
    public class LdargHandler : ICilHandler {
        private static readonly IReadOnlyDictionary<OpCode, int> SpecialMap = new Dictionary<OpCode, int> {
            { OpCodes.Ldarg_0, 0 },
            { OpCodes.Ldarg_1, 1 },
            { OpCodes.Ldarg_2, 2 },
            { OpCodes.Ldarg_3, 3 }
        };

        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Ldarg;
            yield return OpCodes.Ldarg_S;
            foreach (var opCode in SpecialMap.Keys) {
                yield return opCode;
            }
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            int index;
            if (!SpecialMap.TryGetValue(instruction.OpCode, out index))
                index = (int)instruction.Operand;

            context.Stack.Push(GetArgumentValueOrThis(context, index));
        }

        private object GetArgumentValueOrThis(CilHandlerContext context, int index) {
            if (context.Definition.HasThis) {
                if (index == 0)
                    return context.Target;

                index -= 1;
            }

            return context.Arguments[index];
        }
    }
}
