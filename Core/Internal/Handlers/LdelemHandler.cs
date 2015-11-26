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
    public class LdelemHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Ldelem_Any;
            yield return OpCodes.Ldelem_Ref;
            yield return OpCodes.Ldelem_I;
            yield return OpCodes.Ldelem_I1;
            yield return OpCodes.Ldelem_I2;
            yield return OpCodes.Ldelem_I4;
            yield return OpCodes.Ldelem_I8;
            yield return OpCodes.Ldelem_R4;
            yield return OpCodes.Ldelem_R8;
            yield return OpCodes.Ldelem_U1;
            yield return OpCodes.Ldelem_U2;
            yield return OpCodes.Ldelem_U4;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var index = TypeSupport.Convert<IntPtr>(context.Stack.Pop());
            var array = (Array)ObjectWrapper.UnwrapIfRequired(context.Stack.Pop());

            var value = array.GetValue(index.ToInt64());
            context.Stack.Push(value);
        }
    }
}
