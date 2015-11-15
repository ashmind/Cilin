using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.Reflection {
    public interface IInterpretedMethodBase {
        InterpretedType DeclaringType { get; }
        object InvokableDefinition { get; }
    }
}
