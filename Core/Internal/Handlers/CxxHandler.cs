using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Core.Internal.Handlers {
    public class CgtHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Cgt_Un;
            yield return OpCodes.Ceq;
            yield return OpCodes.Clt;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var right = context.Stack.Pop();
            var left = context.Stack.Pop();

            context.Stack.Push(Compare(instruction, left, right) ? 1 : 0);
        }

        private bool Compare(Instruction instruction, object left, object right) {
            switch (instruction.OpCode.Code) {
                case Code.Cgt_Un: return Primitives.IsGreaterThan(left, right);
                case Code.Clt:    return Primitives.IsLessThan(left, right);
                case Code.Ceq:    return Primitives.Equal(left, right);
                default: throw new NotImplementedException();
            }
        }
    }
}
