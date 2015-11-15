using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Internal.Handlers {
    public class CgtHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Cgt_Un;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var value2 = context.Stack.Pop();
            var value1 = context.Stack.Pop();

            context.Stack.Push(Compare(value1, value2) ? 1 : 0);
        }

        private bool Compare(object value1, object value2) {
            if (value1 == null)
                return false;

            if (value2 == null)
                return true;

            return (int)value1 > (int)value2;
        }
    }
}
