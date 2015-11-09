using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.Reflection;

namespace Cilin.Internal.State {
    public class ObjectTypeOverride : ITypeOverride, IObjectWrapper {
        public ObjectTypeOverride(object @object, Type type) {
            if (@object is ObjectTypeOverride)
                throw new ArgumentException($"Attempted to override type twice ({@object}).");

            if (TypeSupport.GetTypeOf(@object) == type)
                throw new ArgumentException($"Attempted to override type for {@object} that already has type {type}.");

            Type = Argument.NotNull(nameof(type), type);
            Object = @object;
        }

        public object Object { get; }
        public Type Type { get; }
        
        public override string ToString() {
            return Object?.ToString();
        }
    }
}
