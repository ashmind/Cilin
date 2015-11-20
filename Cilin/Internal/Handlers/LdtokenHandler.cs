using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilin.Internal.Handlers {
    public class LdtokenHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Ldtoken;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var reference = (MemberReference)instruction.Operand;

            var typeReference = reference as TypeReference;
            if (typeReference == null)
                throw new NotImplementedException();

            var resolved = context.Resolver.Type(typeReference, context.GenericScope);
            var nonRuntime = resolved as NonRuntimeType;
            if (nonRuntime != null){
                context.Stack.Push(nonRuntime.TypeHandle);
                return;
            }

            context.Stack.Push(resolved.TypeHandle);
        }
    }
}
