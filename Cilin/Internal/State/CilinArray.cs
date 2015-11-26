using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.Reflection;
using InfoOf;

namespace Cilin.Internal.State {
    public class CilinArray : INonRuntimeObject, ICustomInvoker, IObjectWrapper {
        private static readonly MethodInfo GetEnumerator = Info.MethodOf<IEnumerable>(e => e.GetEnumerator());

        public CilinArray(int length, InterpretedArrayType arrayType) {
            Array = Array.CreateInstance(typeof(CilinObject), length);
            ArrayType = Argument.NotNull("arrayType", arrayType);
        }

        public object Invoke(MethodBase method, object[] arguments, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
            if (method.Name == nameof(IEnumerable.GetEnumerator))
                return new CilinArrayIterator(this, (NonRuntimeType)((MethodInfo)method).ReturnType);

            throw new NotImplementedException($"Method {method} is not implemented.");
        }

        public Array Array { get; }
        public InterpretedArrayType ArrayType { get;}

        Type INonRuntimeObject.Type => ArrayType;
        object IObjectWrapper.Object => Array;
    }
}
