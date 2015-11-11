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
    public class InterpretedType : Type, INonRuntimeType {
        private readonly string _name;
        private readonly string _namespace;
        private readonly Assembly _assembly;
        private readonly Lazy<Type> _declaringType;
        private readonly Lazy<Type> _baseType;
        private readonly Lazy<Type[]> _interfaces;
        private readonly Type _elementType;
        private readonly IReadOnlyCollection<LazyMember> _members;
        private readonly TypeAttributes _attributes;
        private readonly GenericDetails _generic;
        private Lazy<string> _fullName;

        private bool _staticConstructorStarted;

        public InterpretedType(
            string name,
            string @namespace,
            Assembly assembly,
            Lazy<Type> declaringType,
            Lazy<Type> baseType,
            Lazy<Type[]> interfaces,
            Type elementType,
            IReadOnlyCollection<LazyMember> members,
            TypeAttributes attributes,
            GenericDetails generic
        ) {
            _name = name;
            _namespace = @namespace;
            _assembly = assembly;
            _declaringType = declaringType;
            _baseType = baseType;
            _interfaces = interfaces;
            _elementType = elementType;
            _members = members;
            _attributes = attributes;
            _generic = generic;

            _fullName = new Lazy<string>(GetFullName);

            StaticData = new StaticData(this);
        }

        public void EnsureStaticConstructorRun() {
            if (_staticConstructorStarted)
                return;

            _staticConstructorStarted = true;
            
            var constructor = _members.OfType<LazyMember<ConstructorInfo>>().FirstOrDefault(m => m.IsStatic);
            if (constructor == null)
                return;

            constructor.Info.Invoke(null);
        }

        public StaticData StaticData { get; }

        public override Assembly Assembly => _assembly;

        public override string AssemblyQualifiedName {
            get { throw new NotImplementedException(); }
        }

        public override Type BaseType => _baseType.Value;
        public override string FullName => _fullName.Value;

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

        public override Type GetElementType() => _elementType;

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

        public override Type[] GetGenericArguments() {
            return _generic?.Arguments ?? Empty<Type>.Array;
        }

        public override Type GetInterface(string name, bool ignoreCase) {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces() => _interfaces.Value;

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
            //if (_methods == null) {
            //    _methods = _definition.Methods
            //        .Where(m => !m.IsConstructor)
            //        .Select(m => _resolver.Method(m, GenericScope))
            //        .Cast<MethodInfo>()
            //        .ToArray();
            //}

            //return _methods.Where(m => MemberMatches(m, bindingAttr)).ToArray();
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

        protected override bool HasElementTypeImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsArrayImpl() {
            return false;
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

        public override Type DeclaringType => _declaringType.Value;

        public override Type MakeArrayType() {
            //if (_arrayType != null)
            //    return _arrayType;

            //_arrayType = _resolver.Type(new ArrayType(_reference), null);
            //return _arrayType;
            throw new NotImplementedException();
        }

        public GenericScope GenericScope { get; }

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

        private string GetFullName() {
            if (IsNested)
                return _declaringType.Value.FullName + "." + Name;

            return (Namespace != null) ? Namespace + "." + Name : Name;
        }

        public override string ToString() {
            return FullName;
        }
    }
}
