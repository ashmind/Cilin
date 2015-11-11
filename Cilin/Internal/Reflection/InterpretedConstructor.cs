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
        private readonly MethodDefinition _definition;
        private readonly Lazy<ParameterInfo[]> _parameters;
        private readonly Interpreter _interpreter;
        private readonly InterpretedType _declaringType;
        private readonly Resolver _resolver;

        public InterpretedConstructor(MethodDefinition definition, InterpretedType declaringType, Interpreter interpreter, Resolver resolver) {
            _definition = definition;
            _interpreter = interpreter;
            //_parameters = new Lazy<ParameterInfo[]>(() => {
            //    if (!definition.HasParameters)
            //        return Empty<ParameterInfo>.Array;

            //    return definition.Parameters
            //        .Select(p => _resolver.Parameter(p))
            //        .ToArray();
            //});
            _declaringType = declaringType;
            _resolver = resolver;
        }

        public override MethodAttributes Attributes {
            get { return (MethodAttributes)_definition.Attributes; }
        }

        public override Type DeclaringType {
            get { return _declaringType; }
        }

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

        public override ParameterInfo[] GetParameters() {
            return _parameters.Value;
        }

        public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            var instance = !IsStatic ? new CilinObject(_declaringType) : null;
            Invoke(instance, parameters);
            return instance;
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            if (!IsStatic)
                ((InterpretedType)DeclaringType).EnsureStaticConstructorRun();

            var declaringTypeArguments = (_declaringType.ToTypeReference() as GenericInstanceType)?.GenericArguments.ToArray()
                                      ?? Empty<TypeReference>.Array;
            _interpreter.InterpretCall(declaringTypeArguments, _definition, Empty<TypeReference>.Array, obj, parameters);
            return null;
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }
    }
}
