using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Core.Internal.Reflection;

namespace Cilin.Core.Internal.State {
    public class NonRuntimeMethodPointer : INonRuntimeObject {
        public InterpretedMethod Method { get; }
        public NonRuntimeMethodPointer(InterpretedMethod method) {
            Method = method;
        }

        Type INonRuntimeObject.Type => typeof(IntPtr);
    }
}
