using System;
using System.Globalization;
using System.Reflection;
using Cilin.Internal.State;
using Mono.Cecil;

using FieldAttributes = System.Reflection.FieldAttributes;

namespace Cilin.Internal.Reflection {
    public class InterpretedField : FieldInfo {
        private readonly FieldReference _reference;
        private readonly FieldDefinition _definition;
        private readonly InterpretedType _declaringType;
        private readonly Resolver _resolver;

        public InterpretedField(FieldReference reference, FieldDefinition definition, InterpretedType declaringType, Resolver resolver) {
            _reference = reference;
            _definition = definition;
            _resolver = resolver;
            _declaringType = declaringType;
        }

        public override FieldAttributes Attributes {
            get { return (FieldAttributes)_definition.Attributes; }
        }

        public override Type DeclaringType {
            get { return _declaringType; }
        }

        public override RuntimeFieldHandle FieldHandle {
            get {
                throw new NotImplementedException();
            }
        }

        public override Type FieldType {
            get { return _resolver.Type(_reference.FieldType, _declaringType.GenericScope); }
        }

        public override string Name {
            get { return _definition.Name; }
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

        public override object GetValue(object obj) {
            ((InterpretedType)DeclaringType).EnsureStaticConstructorRun();

            if (IsStatic)
                return ((InterpretedType)DeclaringType).StaticData.Get(this);

            var data = obj as CilinObject;
            if (data == null)
                throw new NotImplementedException();

            return data.Get(this);
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
            ((InterpretedType)DeclaringType).EnsureStaticConstructorRun();

            if (IsStatic) {
                ((InterpretedType)DeclaringType).StaticData.Set(this, value);
                return;
            }

            var data = obj as CilinObject;
            if (data == null)
                throw new NotImplementedException();

            data.Set(this, value);
        }
    }
}