using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.Reflection {
    public class ErasedWrapperType : NonRuntimeType {
        private Type[] _genericArguments;

        public ErasedWrapperType(Type runtimeType, NonRuntimeType fullType) {
            RuntimeType = runtimeType;
            FullType = fullType;
        }

        public Type RuntimeType { get; }
        public NonRuntimeType FullType { get; }

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

        public override Assembly Assembly {
            get {
                throw new NotImplementedException();
            }
        }

        public override string FullName {
            get {
                throw new NotImplementedException();
            }
        }

        public override string Namespace {
            get {
                throw new NotImplementedException();
            }
        }

        public override string AssemblyQualifiedName {
            get {
                throw new NotImplementedException();
            }
        }

        public override Type BaseType {
            get {
                throw new NotImplementedException();
            }
        }

        public override Type UnderlyingSystemType {
            get {
                throw new NotImplementedException();
            }
        }

        public override string Name {
            get {
                throw new NotImplementedException();
            }
        }

        public override Type[] GetGenericArguments() {
            if (_genericArguments == null) {
                var erasedArguments = RuntimeType.GetGenericArguments();
                var fullArguments = FullType.GetGenericArguments();
                var genericArguments = new Type[erasedArguments.Length];
                for (int i = 0; i < erasedArguments.Length; i++) {
                    var full = fullArguments[i];
                    var erased = erasedArguments[i];
                    if (full is ErasedWrapperType || full == erased) {
                        genericArguments[i] = full;
                        continue;
                    }

                    genericArguments[i] = new ErasedWrapperType(erased, (InterpretedType)full);
                }
                _genericArguments = genericArguments;
            }

            return _genericArguments;
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
            return RewriteConstructor(RuntimeType.GetConstructor(bindingAttr, binder, callConvention, types, modifiers));
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) {
            return RewriteConstructors(RuntimeType.GetConstructors(bindingAttr));
        }

        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) {
            return RewriteConstructors(RuntimeType.GetMember(name, type, bindingAttr));
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) {
            return RewriteConstructors(RuntimeType.GetMembers(bindingAttr));
        }

        public override MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria) {
            return RewriteConstructors(RuntimeType.FindMembers(memberType, bindingAttr, filter, filterCriteria));
        }

        private TMember[] RewriteConstructors<TMember>(TMember[] members)
            where TMember: MemberInfo
        {
            for (var i = 0; i < members.Length; i++) {
                var constructor = members[i] as ConstructorInfo;
                if (constructor != null)
                    members[i] = (TMember)(object)new ErasedWrapperConstructor(this, constructor);
            }
            return members;
        }
        
        private TMember RewriteConstructor<TMember>(TMember member)
            where TMember : MemberInfo 
        {
            var constructor = member as ConstructorInfo;
            return constructor != null 
                 ? (TMember)(object)new ErasedWrapperConstructor(this, constructor)
                 : member;
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) {
            throw new NotImplementedException();
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
            throw new NotImplementedException();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
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
            throw new NotImplementedException();
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) {
            throw new NotImplementedException();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr) {
            throw new NotImplementedException();
        }

        protected override TypeAttributes GetAttributeFlagsImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsArrayImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsByRefImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsPointerImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsPrimitiveImpl() {
            throw new NotImplementedException();
        }

        protected override bool IsCOMObjectImpl() {
            throw new NotImplementedException();
        }

        public override Type GetElementType() {
            throw new NotImplementedException();
        }

        protected override bool HasElementTypeImpl() {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(bool inherit) {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }
    }
}
