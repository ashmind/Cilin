using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.Reflection;

namespace Cilin.Internal.State {
    public class CilinObject : BaseData, ITypeOverride {
        public CilinObject(InterpretedType objectType) {
            ObjectType = objectType;
        }

        public InterpretedType ObjectType { get; }
        public object Invoke(InterpretedMethod method, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            if (method.DeclaringType != ObjectType) {
                binder = binder ?? Type.DefaultBinder;
                var methods = ObjectType.GetMethods().Where(m => m.Name == method.Name).ToArray();

                object state;
                method = (InterpretedMethod)binder.BindToMethod(invokeAttr, methods, ref parameters, null, culture, null, out state);
            }

            return method.InvokeNonVirtual(this, invokeAttr, binder, parameters, culture);
        }

        Type ITypeOverride.Type {
            get { return ObjectType; }
        }
    }
}
