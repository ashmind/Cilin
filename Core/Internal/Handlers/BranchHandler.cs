using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Core.Internal {
    public class BranchHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Br;
            yield return OpCodes.Br_S;

            yield return OpCodes.Brfalse;
            yield return OpCodes.Brfalse_S;
            yield return OpCodes.Brtrue;
            yield return OpCodes.Brtrue_S;

            yield return OpCodes.Bne_Un_S;
            yield return OpCodes.Beq_S;
            yield return OpCodes.Blt_S;
            yield return OpCodes.Ble_S;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            if (!IsConditionTrue(instruction, context))
                return;

            var target = (Instruction)instruction.Operand;
            context.NextInstruction = target;
        }

        private bool IsConditionTrue(Instruction instruction, CilHandlerContext context) {
            switch (instruction.OpCode.Code) {
                case Code.Br:
                case Code.Br_S:
                    return true;

                case Code.Brtrue:
                case Code.Brtrue_S:
                    return TypeSupport.Convert<bool>(context.Stack.Pop());

                case Code.Brfalse:
                case Code.Brfalse_S:
                    return !TypeSupport.Convert<bool>(context.Stack.Pop());

                case Code.Beq_S: return IsBinaryConditionTrue(Primitives.Equal, context);
                case Code.Bne_Un_S: return IsBinaryConditionTrue(Primitives.NotEqual, context);
                case Code.Blt_S: return IsBinaryConditionTrue(Primitives.IsLessThan, context);
                case Code.Ble_S: return IsBinaryConditionTrue(Primitives.IsLessThanOrEqual, context);

                default:
                    throw new NotImplementedException();
            }
        }

        private bool IsBinaryConditionTrue(Func<object, object, bool> isTrue, CilHandlerContext context) {
            var right = context.Stack.Pop();
            var left = context.Stack.Pop();
            return isTrue(left, right);
        }
    }
}
