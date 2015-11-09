using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Cilin.Internal.Reflection {
    public class InterpretedAssembly : Assembly {
        private AssemblyDefinition _definition;

        public InterpretedAssembly(AssemblyDefinition definition) {
            _definition = definition;
        }
    }
}
