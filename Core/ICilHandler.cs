using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Cilin.Core {
    public interface ICilHandler {
        IEnumerable<OpCode> GetOpCodes();
        void Handle(Instruction instruction, CilHandlerContext context);
    }
}