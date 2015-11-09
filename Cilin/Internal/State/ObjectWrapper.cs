using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.State {
    public static class ObjectWrapper {
        public static object UnwrapIfRequired(object @object) {
            var wrapper = @object as IObjectWrapper;
            if (wrapper != null)
                return wrapper.Object;

            return @object;
        }
    }
}
