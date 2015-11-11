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
    public class InterpretedConstructor : ConstructorInfo {
        private readonly InterpretedType _declaringType;
        private readonly ParameterInfo[] _parameters;
        private readonly MethodAttributes _attributes;
        private readonly Func<object, object[], object> _invoke;

        public InterpretedConstructor(
            InterpretedType declaringType,
            ParameterInfo[] parameters,
            MethodAttributes attributes,
            Func<object, object[], object> invoke
        ) {
            _declaringType = declaringType;
            _parameters = parameters;
            _attributes = attributes;
            _invoke = invoke;
        }

        public override MethodAttributes Attributes => _attributes;
        public override Type DeclaringType => _declaringType;
        
        public override RuntimeMethodHandle MethodHandle {
            get {
                throw new NotImplementedException();
            }
        }

        public override string Name {
            get {
                throw new NotImplementedException();
            }
        }

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

        public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags() {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters() => _parameters;

        public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            var instance = !IsStatic ? new CilinObject(_declaringType) : null;
            Invoke(instance, parameters);
            return instance;
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            if (!IsStatic)
                ((InterpretedType)DeclaringType).EnsureStaticConstructorRun();

            return _invoke(obj, parameters);
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }
    }
}
