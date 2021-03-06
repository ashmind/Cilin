﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Cilin.Core.Internal.Reflection;

namespace Cilin.Core.Internal.State {
    public class CilinDelegate : INonRuntimeObject, ICustomInvoker {
        public CilinDelegate(object target, NonRuntimeMethodPointer pointer, Type delegateType) {
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
        public NonRuntimeMethodPointer Pointer { get; }
        public Type DelegateType { get; }

        Type INonRuntimeObject.Type => DelegateType;
    }
}
