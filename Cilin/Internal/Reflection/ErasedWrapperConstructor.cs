using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.State;

namespace Cilin.Internal.Reflection {
    public class ErasedWrapperConstructor : ConstructorInfo {
        private readonly ErasedWrapperType _wrapperType;
        private readonly ConstructorInfo _runtimeConstructor;

        public ErasedWrapperConstructor(ErasedWrapperType wrapperType, ConstructorInfo runtimeConstructor) {
            _wrapperType = wrapperType;
            _runtimeConstructor = runtimeConstructor;
        }

        public override int MetadataToken {
            get { return _runtimeConstructor.MetadataToken; }
        }

        public override MethodAttributes Attributes => _runtimeConstructor.Attributes;
        public override Type DeclaringType => _wrapperType;

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

        public override MethodImplAttributes GetMethodImplementationFlags() {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters() {
            return _runtimeConstructor.GetParameters();
        }

        public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            var runtimeObject = _runtimeConstructor.Invoke(invokeAttr, binder, parameters, culture);
            return new ObjectTypeOverride(runtimeObject, _wrapperType.FullType);
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            return Invoke(parameters);
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }
    }
}
