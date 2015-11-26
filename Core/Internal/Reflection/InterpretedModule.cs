using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Core.Internal.Reflection {
    public class InterpretedModule : Module {
        private readonly InterpretedAssembly _assembly;
        public InterpretedModule(InterpretedAssembly assembly) {
            _assembly = assembly;
        }

        public override Assembly Assembly => _assembly;
    }
}
