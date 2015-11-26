using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.Reflection {
    public class InterpretedGenericPathType : InterpretedType {
        private readonly Type _genericDefinition;
        private readonly Type[] _genericArguments;

        public InterpretedGenericPathType(
            Type genericDefinition,
            Lazy<Type> baseType,
            Lazy<Type[]> interfaces,
            Func<Type, IReadOnlyCollection<ILazyMember<MemberInfo>>> getMembers,
            Type[] genericArguments
        ) : base(baseType, interfaces, getMembers) {
            _genericDefinition = genericDefinition;
            _genericArguments = genericArguments;
        }

        public override Assembly Assembly {
            get {
                throw new NotImplementedException();
            }
        }

        public override Type[] GenericTypeArguments => _genericArguments;

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

        public override string Name => _genericDefinition.Name;

        public override string Namespace {
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

        public override Type[] GetGenericArguments() => _genericArguments;

        public override Type GetInterface(string name, bool ignoreCase) {
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

        protected override TypeAttributes GetAttributeFlagsImpl() => _genericDefinition.Attributes;

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

        public override bool IsConstructedGenericType => true;

        protected override bool IsPointerImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsPrimitiveImpl() {
            throw new NotImplementedException();
        }

        protected override string GetFullName() {
            var builder = new StringBuilder();
            builder.Append(_genericDefinition.FullName);
            if (_genericArguments.Length == 0)
                return builder.ToString();
            builder.Append("[[")
                    .Append(_genericArguments[0].AssemblyQualifiedName);

            for (var i = 1; i < _genericArguments.Length; i++) {
                builder.Append("],[")
                        .Append(_genericArguments[i].AssemblyQualifiedName);
            }
            builder.Append("]]");
            return builder.ToString();
        }

    }
}
