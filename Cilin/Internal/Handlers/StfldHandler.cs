using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilin.Internal {
    public class StfldHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Stfld;
            yield return OpCodes.Stsfld;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var field = context.Resolver.Field((FieldReference)instruction.Operand, context.GenericScope);

            var value = TypeSupport.Convert(context.Stack.Pop(), field.FieldType);

            var target = (object)null;
            if (instruction.OpCode == OpCodes.Stfld)
                target = TypeSupport.Convert(context.Stack.Pop(), field.DeclaringType);

            field.SetValue(target, value);
        }
    }
}
