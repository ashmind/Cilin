using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.Reflection {
    public class GenericParameterType : NonRuntimeType {
        private readonly string _name;

        public GenericParameterType(string name) {
            _name = name;
        }

        public override Assembly Assembly {
            get {
                throw new NotImplementedException();
            }
        }

        public override string AssemblyQualifiedName => null;
        public override Type BaseType => typeof(object);
        public override string FullName => null;

        public override Guid GUID {
            get {
                throw new NotImplementedException();
            }
        }

        public override Module Module {
            get {
                throw new NotImplementedException();
            }
        }

        public override string Name => _name;

        public override string Namespace {
            get {
                throw new NotImplementedException();
            }
        }

        public override Type UnderlyingSystemType => this;

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(bool inherit) {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }

        public override Type GetElementType() {
            throw new NotImplementedException();
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override Type[] GetGenericParameterConstraints() => EmptyTypes;

        public override Type GetInterface(string name, bool ignoreCase) {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces() {
            throw new NotImplementedException();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }

        protected override TypeAttributes GetAttributeFlagsImpl() => TypeAttributes.Public;

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
            throw new NotImplementedException();
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
            throw new NotImplementedException();
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) {
            throw new NotImplementedException();
        }

        protected override bool HasElementTypeImpl() => false;

        protected override bool IsArrayImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsByRefImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsCOMObjectImpl() {
            throw new NotImplementedException();
        }

        public override bool IsGenericParameter => true;

        protected override bool IsPointerImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsPrimitiveImpl() {
            throw new NotImplementedException();
        }
    }
}
