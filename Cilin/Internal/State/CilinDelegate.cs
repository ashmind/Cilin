using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.Reflection;

namespace Cilin.Internal.State {
    public class CilinDelegate : ITypeOverride, ICustomInvoker {
        public CilinDelegate(object target, MethodPointerWrapper pointer, Type delegateType) {
            Target = target;
            Pointer = pointer;
            DelegateType = delegateType;
        }

        public object Invoke(MethodBase method, object[] arguments, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
            if (method.Name == ConstructorInfo.ConstructorName)
                return null; // covered by constructor itself

            if (method.Name == nameof(Action.Invoke))
                return Pointer.Method.Invoke(Target, arguments);

            throw new NotSupportedException($"Delegate method {method.Name} is not currently supported.");
        }

        public object Target { get; }
        public MethodPointerWrapper Pointer { get; }
        public Type DelegateType { get; }

        Type ITypeOverride.Type => DelegateType;
    }
}
