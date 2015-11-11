using System;
using System.Globalization;
using System.Reflection;
using Cilin.Internal.State;
using Mono.Cecil;

using FieldAttributes = System.Reflection.FieldAttributes;

namespace Cilin.Internal.Reflection {
    public class InterpretedField : FieldInfo {
        private readonly InterpretedType _declaringType;
        private readonly string _name;
        private readonly Type _fieldType;
        private readonly FieldAttributes _attributes;

        public InterpretedField(InterpretedType declaringType, string name, Type fieldType, FieldAttributes attributes) {
            _declaringType = declaringType;
            _name = name;
            _fieldType = fieldType;
            _attributes = attributes;
        }

        public override FieldAttributes Attributes => _attributes;
        public override Type DeclaringType => _declaringType;

        public override RuntimeFieldHandle FieldHandle {
            get {
                throw new NotImplementedException();
            }
        }

        public override Type FieldType => _fieldType;
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