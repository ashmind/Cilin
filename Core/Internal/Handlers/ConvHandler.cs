using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Cilin.Core.Internal.Handlers {
    public class ConvHandler : ICilHandler {
        private static readonly IReadOnlyDictionary<OpCode, Type> TypeMap = new Dictionary<OpCode, Type> {
            { OpCodes.Conv_I4, typeof(int) }
        };

        public IEnumerable<OpCode> GetOpCodes() {
            return TypeMap.Keys;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var converted = Primitives.Convert(context.Stack.Pop(), TypeMap[instruction.OpCode]);
            context.Stack.Push(converted);
        }
    }
}
