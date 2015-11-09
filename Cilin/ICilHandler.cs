using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Cilin {
    public interface ICilHandler {
        IEnumerable<OpCode> GetOpCodes();
        void Handle(Instruction instruction, CilHandlerContext context);
    }
}