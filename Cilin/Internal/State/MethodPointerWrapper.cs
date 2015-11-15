using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.Reflection;

namespace Cilin.Internal.State {
    public class MethodPointerWrapper {
        public InterpretedMethod Method { get; }
        public MethodPointerWrapper(InterpretedMethod method) {
            Method = method;
        }
    }
}
