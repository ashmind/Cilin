using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Core.Internal.Reflection {
    public delegate object Invoke(
        MethodBase method, object target, object[] arguments, BindingFlags invokeAttr, Binder binder, CultureInfo culture
    );
}
