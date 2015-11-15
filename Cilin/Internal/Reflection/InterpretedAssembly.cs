using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Cilin.Internal.Reflection {
    public class InterpretedAssembly : Assembly {
        private string _fullName;

        public InterpretedAssembly(string fullName) {
            _fullName = fullName;
        }

        public override string FullName => _fullName;
    }
}
