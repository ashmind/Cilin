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
    public class InterpretedType : Type {
        private readonly TypeReference _reference;
        private readonly TypeDefinition _definition;
        private readonly Resolver _resolver;

        private bool _staticConstructorStarted;

        private Type _baseType;
        private Type _arrayType;
        private string _fullName;
        private Type[] _interfaces;
        private MethodInfo[] _methods;
        private FieldInfo[] _fields;

        public InterpretedType(TypeReference reference, TypeDefinition definition, Interpreter interpreter, Resolver resolver) {
            _reference = reference;
            _definition = definition;
            _resolver = resolver;
            Interpreter = interpreter;

            var generic = reference as GenericInstanceType;
            if (generic != null) {
                GenericScope = new GenericScope(_definition, generic.GenericArguments.ToArray());
            }
            else if (_reference.HasGenericParameters) {
                throw new ArgumentException($"{nameof(InterpretedType)} cannot have unresolved generic parameters (found: {_reference.GenericParameters.First()}).");
            }

            if (!IsInterface && !IsEnum)
                StaticData = new StaticData(this);
        }

        public void EnsureStaticConstructorRun() {
            if (_staticConstructorStarted)
                return;

            _staticConstructorStarted = true;

            if (!_definition.HasMethods)
                return;

            var constructorDefinition = _definition.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);
            if (constructorDefinition == null)
                return;

            var constructor = (ConstructorInfo)_resolver.Method(constructorDefinition);
            constructor.Invoke(null);
        }

        public StaticData StaticData { get; }

        public override Assembly Assembly {
            get { return _resolver.Assembly(_definition.Module.Assembly); }
        }

        public override string AssemblyQualifiedName {
            get { throw new NotImplementedException(); }
        }

        public override Type BaseType {
            get {
                if (_baseType == null)
                    _baseType = GetBaseTypeUncached();

                return _baseType;
            }
        }

        private Type GetBaseTypeUncached() {
            if (_reference.IsArray)
                return typeof(Array);

            return _resolver.Type(_definition.BaseType, GenericScope);
        }

        public override string FullName {
            get {
                if (_fullName == null)
                    _fullName = TypeSupport.GetFullName(_reference);

                return _fullName;
            }
        }

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

        public override string Name {
            get { return _reference.Name; }
        }

        public override string Namespace {
            get { return _definition.Namespace; }
        }

        public override Type UnderlyingSystemType {
            get { return this; }
        }

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
            var elementType = _definition.GetElementType();
            return elementType != null ? _resolver.Type(elementType) : null;
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

        public override Type GetInterface(string name, bool ignoreCase) {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces() {
            if (_interfaces == null) {
                var interfaceReferences = _definition.Interfaces.AsEnumerable();
                if (_reference.IsArray)
                    interfaceReferences = TypeSupport.GetArrayInterfaces((ArrayType)_reference);

                var interfaces = interfaceReferences.Select(t => _resolver.Type(t, GenericScope));
                interfaces = interfaces.Concat(BaseType.GetInterfaces());
                _interfaces = interfaces.ToArray();
            }

            return _interfaces;
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
            if (_methods == null) {
                _methods = _definition.Methods
                    .Where(m => !m.IsConstructor)
                    .Select(m => _resolver.Method(m, GenericScope, this))
                    .Cast<MethodInfo>()
                    .ToArray();
            }

            return _methods.Where(m => MemberMatches(m, bindingAttr)).ToArray();
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
            if (IsArray)
                return typeof(Array).Attributes;

            return (TypeAttributes)_definition.Attributes;
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

        protected override bool HasElementTypeImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsArrayImpl() {
            return _reference.IsArray;
        }

        protected override bool IsByRefImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsCOMObjectImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsPointerImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsPrimitiveImpl() {
            throw new NotImplementedException();
        }

        public override bool IsInstanceOfType(object o) {
            if (o == null)
                return false;

            return IsAssignableFrom(TypeSupport.GetTypeOf(o));
        }

        public override bool IsAssignableFrom(Type c) {
            if (c == null)
                return false;

            if (this == c)
                return true;

            var erased = c as ErasedWrapperType;
            if (erased != null)
                return IsAssignableFrom(erased.FullType);

            if (c.IsSubclassOf(this))
                return true;

            if (IsInterface) {
                var interfaces = c.GetInterfaces();
                foreach (var @interface in interfaces) {
                    if (((@interface as ErasedWrapperType)?.FullType ?? @interface) == this)
                        return true;
                }
            }

            return false;
        }

        public override Type DeclaringType {
            get { return _definition.DeclaringType != null ? _resolver.Type(_definition.DeclaringType) : null; }
        }

        public override Type MakeArrayType() {
            if (_arrayType != null)
                return _arrayType;

            _arrayType = _resolver.Type(new ArrayType(_reference));
            return _arrayType;
        }

        public Interpreter Interpreter { get; }
        public GenericScope GenericScope { get; }

        private void EnsureFields() {
            if (_fields != null)
                return;

            if (!_definition.HasFields)
                _fields = Empty<FieldInfo>.Array;

            _fields = _definition.Fields.Select(f => _resolver.Field(f, GenericScope)).ToArray();
        }

        private bool MemberMatches(MethodInfo method, BindingFlags bindingAttr) {
            var unsupported = BindingFlags.ExactBinding
                            | BindingFlags.FlattenHierarchy
                            | BindingFlags.GetField
                            | BindingFlags.GetProperty
                            | BindingFlags.IgnoreCase
                            | BindingFlags.IgnoreReturn
                            | BindingFlags.InvokeMethod
                            | BindingFlags.OptionalParamBinding
                            | BindingFlags.PutDispProperty
                            | BindingFlags.PutRefDispProperty
                            | BindingFlags.SetField
                            | BindingFlags.SetProperty
                            | BindingFlags.SuppressChangeType;
            if ((bindingAttr & unsupported) != 0)
                throw new NotImplementedException();

            if ((bindingAttr & BindingFlags.DeclaredOnly) != 0 && method.DeclaringType != this)
                return false;

            if ((bindingAttr & BindingFlags.Public) == 0 && method.IsPublic)
                return false;

            if ((bindingAttr & BindingFlags.NonPublic) == 0 && !method.IsPublic)
                return false;

            if ((bindingAttr & BindingFlags.Instance) == 0 && !method.IsStatic)
                return false;

            if ((bindingAttr & BindingFlags.Static) == 0 && method.IsStatic)
                return false;

            return true;
        }

        public TypeReference ToTypeReference() {
            return _reference;
        }

        public override string ToString() {
            return FullName;
        }
    }
}
