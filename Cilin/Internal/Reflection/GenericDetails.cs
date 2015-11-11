using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.Reflection {
    public class GenericDetails {
        public GenericDetails(Type[] arguments) {
            Arguments = arguments;
        }

        public Type[] Arguments { get; }
    }
}
