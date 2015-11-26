using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AshMind.Extensions;
using Cilin.Internal.State;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilin.Internal {
    public class LdelemHandler : ICilHandler {
        private static readonly IReadOnlyDictionary<OpCode, Type> TypeMap = new Dictionary<OpCode, Type> {
            { OpCodes.Ldelem_I,  typeof(IntPtr) },
            { OpCodes.Ldelem_I1, typeof(sbyte) },
            { OpCodes.Ldelem_I2, typeof(short) },
            { OpCodes.Ldelem_I4, typeof(int) },
            { OpCodes.Ldelem_I8, typeof(long) },
            { OpCodes.Ldelem_R4, typeof(float) },
            { OpCodes.Ldelem_R8, typeof(double) }
        };

        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Ldelem_Any;
            yield return OpCodes.Ldelem_Ref;
            foreach (var opCode in TypeMap.Keys) {
                yield return opCode;
            }
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var type = TypeMap.GetValueOrDefault(instruction.OpCode) ?? (
                instruction.OpCode == OpCodes.Ldelem_Any
                    ? context.Resolver.Type((TypeReference)instruction.Operand, context.GenericScope)
                    : typeof(object)
            );
            
            var index = TypeSupport.Convert<IntPtr>(context.Stack.Pop());
            var array = (Array)ObjectWrapper.UnwrapIfRequired(context.Stack.Pop());

            var value = array.GetValue(index.ToInt64());
            context.Stack.Push(value);
        }
    }
}
