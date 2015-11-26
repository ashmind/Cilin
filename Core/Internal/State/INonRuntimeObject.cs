using System;
using System.Collections.Generic;
using System.Linq;
using Cilin.Core.Internal.Reflection;

namespace Cilin.Core.Internal.State {
    public interface INonRuntimeObject {
        Type Type { get; }
    }
}
