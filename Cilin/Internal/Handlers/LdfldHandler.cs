using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilin.Internal {
    public class LdfldHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Ldfld;
            yield return OpCodes.Ldsfld;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var field = context.Resolver.Field((FieldReference)instruction.Operand, context.GenericScope);

            var target = (object)null;
            if (instruction.OpCode == OpCodes.Ldfld)
                target = TypeSupport.Convert(context.Stack.Pop(), field.DeclaringType);

            var value = field.GetValue(target);
            context.Stack.Push(value);
        }
    }
}
