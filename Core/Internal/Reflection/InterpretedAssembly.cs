using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Cilin.Core.Internal.Reflection {
    public class InterpretedAssembly : Assembly {
        private readonly string _fullName;
        private readonly string _location;

        public InterpretedAssembly(string fullName, string location) {
            _fullName = fullName;
            _location = location;
        }

        public override string FullName => _fullName;
        public override string Location => _location;
    }
}
