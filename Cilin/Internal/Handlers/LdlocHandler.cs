﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Internal {
    public class LdlocHandler : ICilHandler {
        private readonly IReadOnlyDictionary<OpCode, int> SpecialMap = new Dictionary<OpCode, int> {
            { OpCodes.Ldloc_0, 0 },
            { OpCodes.Ldloc_1, 1 },
            { OpCodes.Ldloc_2, 2 },
            { OpCodes.Ldloc_3, 3 }
        };

        public IEnumerable<OpCode> GetOpCodes() {
            foreach (var opCode in SpecialMap.Keys) {
                yield return opCode;
            }
            yield return OpCodes.Ldloc;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            int index;
            if (!SpecialMap.TryGetValue(instruction.OpCode, out index))
                index = (int)instruction.Operand;

            context.Stack.Push(context.Variables[index]);
        }
    }
}