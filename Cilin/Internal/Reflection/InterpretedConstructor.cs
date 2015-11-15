using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.State;
using Mono.Cecil;

using MethodAttributes = System.Reflection.MethodAttributes;

namespace Cilin.Internal.Reflection {
    public class InterpretedConstructor : ConstructorInfo, IInterpretedMethodBase {
        private readonly InterpretedType _declaringType;
        private readonly string _name;
        private readonly ParameterInfo[] _parameters;
        private readonly MethodAttributes _attributes;
        private readonly Func<object[], object> _create;
        private readonly MethodInvoker _invoker;

        public InterpretedConstructor(
            InterpretedType declaringType,
            string name,
            ParameterInfo[] parameters,
            MethodAttributes attributes,
            Func<object[], object> create,
            MethodInvoker invoker,
            object invokableDefinition
        ) {
            _declaringType = declaringType;
            _name = name;
            _parameters = parameters;
            _attributes = attributes;
            _create = create;
            _invoker = invoker;
            InvokableDefinition = invokableDefinition;
        }

        public override MethodAttributes Attributes => _attributes;
        public override Type DeclaringType => _declaringType;
        
        public override RuntimeMethodHandle MethodHandle {
            get {
                throw new NotImplementedException();
            }
        }

        public override string Name => _name;

        public override Type ReflectedType {
            get {
                throw new NotImplementedException();
            }
        }

        public override object[] GetCustomAttributes(bool inherit) {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }

        public override Type[] GetGenericArguments() => Type.EmptyTypes;

        public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags() {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters() => _parameters;

        public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            var instance = !IsStatic ? _create(parameters) : null;
            Invoke(instance, parameters);
            return instance;
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            return _invoker.Invoke(this, obj, parameters, invokeAttr, binder, culture);
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }

        public object InvokableDefinition { get; }

        InterpretedType IInterpretedMethodBase.DeclaringType => _declaringType;
    }
}
