using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AshMind.Extensions;
using Cilin.Internal.State;
using Mono.Cecil;

using TypeAttributes = System.Reflection.TypeAttributes;

namespace Cilin.Internal.Reflection {
    public class InterpretedDefinitionType : InterpretedType {
        private readonly string _name;
        private readonly string _namespace;
        private readonly Assembly _assembly;
        private readonly Type _declaringType;
        private readonly TypeAttributes _attributes;
        private readonly Type[] _genericParameters;

        public InterpretedDefinitionType(
            string name,
            string @namespace,
            Assembly assembly,
            Type declaringType,
            Lazy<Type> baseType,
            Lazy<Type[]> interfaces,
            Func<Type, IReadOnlyCollection<LazyMember>> getMembers,
            TypeAttributes attributes,
            Type[] genericParameters
        ) : base(baseType, interfaces, getMembers) {
            _name = name;
            _namespace = @namespace;
            _assembly = assembly;
            _declaringType = declaringType;
            _attributes = attributes;
            _genericParameters = genericParameters;
        }

        public override Assembly Assembly => _assembly;

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
        public override string Namespace => _namespace;
        
        public override object[] GetCustomAttributes(bool inherit) {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }

        public override Type GetElementType() => null;

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

        public override Type[] GetGenericArguments() => _genericParameters;

        public override Type GetInterface(string name, bool ignoreCase) {
            throw new NotImplementedException();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) {
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

        protected override TypeAttributes GetAttributeFlagsImpl() {
            return _attributes;
        }

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
        protected override bool IsArrayImpl() => false;
        protected override bool IsByRefImpl() => false;

        protected override bool IsCOMObjectImpl() {
            throw new NotImplementedException();
        }

        public override bool IsConstructedGenericType => false;
        public override bool IsGenericType => _genericParameters.Length > 0;

        protected override bool IsPointerImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsPrimitiveImpl() => false;

        public override bool IsInstanceOfType(object o) {
            if (o == null)
                return false;

            return IsAssignableFrom(TypeSupport.GetTypeOf(o));
        }

        public override bool IsAssignableFrom(Type c) {
            if (c == null)
                return false;

            return TypeSupport.IsAssignableFrom(this, c);
        }

        public override Type DeclaringType => _declaringType;

        public override Type MakeArrayType() {
            //if (_arrayType != null)
            //    return _arrayType;

            //_arrayType = _resolver.Type(new ArrayType(_reference), null);
            //return _arrayType;
            throw new NotImplementedException();
        }

        protected override string GetFullName() {
            if (IsNested)
                return DeclaringType.FullName + "+" + Name;

            if (!Namespace.IsNullOrEmpty())
                return Namespace + "." + Name;

            return Name;
        }
    }
}
