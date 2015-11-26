using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AshMind.Extensions;
using Mono.Cecil.Cil;

namespace Cilin.Core.Internal {
    public class LdcHandler : ICilHandler {
        private static readonly IReadOnlyDictionary<OpCode, object> SpecialMap = new Dictionary<OpCode, object> {
            { OpCodes.Ldc_I4_0, 0 },
            { OpCodes.Ldc_I4_1, 1 },
            { OpCodes.Ldc_I4_2, 2 },
            { OpCodes.Ldc_I4_3, 3 },
            { OpCodes.Ldc_I4_4, 4 },
            { OpCodes.Ldc_I4_5, 5 },
            { OpCodes.Ldc_I4_6, 6 },
            { OpCodes.Ldc_I4_7, 7 },
            { OpCodes.Ldc_I4_8, 8 },
            { OpCodes.Ldc_I4_M1, -1 },
            { OpCodes.Ldnull, null }
        };

        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Ldc_I4;
            yield return OpCodes.Ldc_I4_S;
            yield return OpCodes.Ldstr;
            foreach (var opCode in SpecialMap.Keys) {
                yield return opCode;
            }
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var special = SpecialMap.GetValueOrDefault(instruction.OpCode);
            if (special != null) {
                context.Stack.Push(special);
                return;
            }

            context.Stack.Push(instruction.Operand);
        }
    }
}