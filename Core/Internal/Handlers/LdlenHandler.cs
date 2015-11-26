using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AshMind.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Cilin.Core.Internal.State;

namespace Cilin.Core.Internal {
    public class LdlenHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Ldlen;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var array = (Array)ObjectWrapper.UnwrapIfRequired(context.Stack.Pop());
            var value = array.GetLength(0);
            context.Stack.Push(value);
        }
    }
}
