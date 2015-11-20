using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.Reflection {
    public class NonRuntimeHandle {
        public NonRuntimeHandle(MemberInfo member) {
            Member = Argument.NotNull("member", member);
        }

        public MemberInfo Member { get; }
    }
}
