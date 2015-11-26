using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Cilin.Core.Internal.Reflection;

namespace Cilin.Core.Internal.State {
    public class CilinArrayIterator : INonRuntimeObject, ICustomInvoker {
        private readonly CilinArray _array;
        private readonly NonRuntimeType _iteratorType;
        private int _index = -1;

        public CilinArrayIterator(CilinArray array, NonRuntimeType iteratorType) {
            _array = array;
            _iteratorType = iteratorType;
        }

        Type INonRuntimeObject.Type => _iteratorType;

        public object Invoke(MethodBase method, object[] arguments, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
            switch (method.Name) {
                case nameof(IEnumerator.MoveNext):
                    _index += 1;
                    return _index <= _array.Array.GetUpperBound(0);

                case "get_" + nameof(IEnumerator.Current):
                    return _array.Array.GetValue(_index);
            }

            throw new MissingMethodException("<CilinArrayIterator>:" + method.DeclaringType.Name, method.Name);
        }
    }
}
