using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Core.Internal {
    public class MathHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Add;
            yield return OpCodes.Div;
            yield return OpCodes.Mul;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var right = context.Stack.Pop();
            var left = context.Stack.Pop();
            context.Stack.Push(Calculate(instruction, left, right));
        }

        private object Calculate(Instruction instruction, object left, object right) {
            switch (instruction.OpCode.Code) {
                case Code.Add: return Primitives.Add(left, right);
                case Code.Div: return Primitives.Divide(left, right);
                case Code.Mul: return Primitives.Multiply(left, right);
                default: throw new NotImplementedException();
            }
        }
    }
}
