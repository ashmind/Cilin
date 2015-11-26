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
    public class StelemHandler : ICilHandler {
        private static readonly IReadOnlyDictionary<OpCode, Type> TypeMap = new Dictionary<OpCode, Type> {
            { OpCodes.Stelem_I,  typeof(IntPtr) },
            { OpCodes.Stelem_I1, typeof(sbyte) },
            { OpCodes.Stelem_I2, typeof(short) },
            { OpCodes.Stelem_I4, typeof(int) },
            { OpCodes.Stelem_I8, typeof(long) },
            { OpCodes.Stelem_R4, typeof(float) },
            { OpCodes.Stelem_R8, typeof(double) }
        };

        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Stelem_Any;
            yield return OpCodes.Stelem_Ref;
            foreach (var opCode in TypeMap.Keys) {
                yield return opCode;
            }
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var value = context.Stack.Pop();
            var index = TypeSupport.Convert<IntPtr>(context.Stack.Pop());
            var array = (Array)ObjectWrapper.UnwrapIfRequired(context.Stack.Pop());

            array.SetValue(
                TypeSupport.Convert(value, array.GetType().GetElementType()),
                index.ToInt64()
            );
        }
    }
}
