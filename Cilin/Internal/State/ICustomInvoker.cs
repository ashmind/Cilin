using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.State {
    public interface ICustomInvoker {
        object Invoke(MethodBase method, object[] arguments, BindingFlags invokeAttr, Binder binder, CultureInfo culture);
    }
}
