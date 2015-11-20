using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.Reflection;

namespace Cilin.Internal.State {
    public interface INonRuntimeObject {
        Type Type { get; }
    }
}
