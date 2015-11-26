using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Core.Internal.Reflection;

namespace Cilin.Core.Internal.State {
    public class ObjectTypeOverride : INonRuntimeObject, IObjectWrapper {
        public ObjectTypeOverride(object @object, NonRuntimeType type) {
            if (@object is INonRuntimeObject)
                throw new ArgumentException($"Attempted to override type twice ({@object}).");

            if (TypeSupport.GetTypeOf(@object) == type)
                throw new ArgumentException($"Attempted to override type for {@object} that already has type {type}.");

            Type = Argument.NotNull(nameof(type), type);
            Object = @object;
        }

        public object Object { get; }
        public Type Type { get; }

        public override string ToString() => Object?.ToString();
    }
}
