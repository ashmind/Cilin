using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.State;

namespace Cilin.Internal.Reflection {
    public class NonRuntimeHandle : INonRuntimeObject {
        private readonly Type _runtimeHandleType;

        public NonRuntimeHandle(MemberInfo member, Type runtimeHandleType) {
            Member = Argument.NotNull(nameof(member), member);
            _runtimeHandleType = Argument.NotNull(nameof(runtimeHandleType), runtimeHandleType);
        }

        public MemberInfo Member { get; }

        Type INonRuntimeObject.Type => _runtimeHandleType;
    }
}
