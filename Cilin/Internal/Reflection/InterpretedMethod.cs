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
using MethodImplAttributes = System.Reflection.MethodImplAttributes;

namespace Cilin.Internal.Reflection {
    public class InterpretedMethod : MethodInfo {
        private readonly MethodReference _reference;
        private readonly MethodDefinition _definition;
        private readonly Lazy<ParameterInfo[]> _parameters;
        private readonly InterpretedType _declaringType;
        private readonly Interpreter _interpreter;
        private readonly Resolver _resolver;

        public InterpretedMethod(MethodReference reference, MethodDefinition definition, InterpretedType declaringType, Interpreter interpreter, Resolver resolver) {
            Argument.NotNullAndCast<InterpretedType>(nameof(declaringType), declaringType);
            _reference = reference;
            _definition = definition;
            
            _parameters = new Lazy<ParameterInfo[]>(() => {
                if (!_reference.HasParameters)
                    return Empty<ParameterInfo>.Array;

                return _reference.Parameters
                    .Select(p => _resolver.Parameter(p, GenericScope))
                    .ToArray();
            });
            _interpreter = interpreter;
            _declaringType = declaringType;
            _resolver = resolver;

            var generic = reference as GenericInstanceMethod;
            if (generic != null)
                GenericScope = new GenericScope(_definition, generic.GenericArguments.ToArray(), declaringType.GenericScope);
        }

        public override MethodAttributes Attributes {
            get { return (MethodAttributes)_definition.Attributes; }
        }

        public override Type DeclaringType {
            get { return _declaringType; }
        }

        public override RuntimeMethodHandle MethodHandle {
            get { return Delegator.GetMethod(this).MethodHandle; }
        }

        public override string Name {
            get { return _definition.Name; }
        }

        public override Type ReflectedType {
            get {
                throw new NotImplementedException();
            }
        }

        public override System.Reflection.ICustomAttributeProvider ReturnTypeCustomAttributes {
            get {
                throw new NotImplementedException();
            }
        }

        public override MethodInfo GetBaseDefinition() {
            throw new NotImplementedException();
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
            return _parameters.Value;
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            if (IsStatic)
                return InvokeNonVirtual(obj, invokeAttr, binder, parameters, culture);

            return ((CilinObject)ObjectWrapper.UnwrapIfRequired(obj))
                .Invoke(this, invokeAttr, binder, parameters, culture);
        }

        public object InvokeNonVirtual(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            ((InterpretedType)DeclaringType).EnsureStaticConstructorRun();
            var declaringTypeArguments = (_declaringType.ToTypeReference() as GenericInstanceType)?.GenericArguments.ToArray()
                                       ?? Empty<TypeReference>.Array;
            var typeArguments = (_reference as GenericInstanceMethod)?.GenericArguments.ToArray()
                             ?? Empty<TypeReference>.Array;

            return _interpreter.InterpretCall(declaringTypeArguments, _definition, typeArguments, obj, parameters);
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }

        public override Type ReturnType {
            get { return _resolver.Type(_reference.ReturnType, GenericScope); }
        }
        
        public GenericScope GenericScope { get; }

        private class Delegator {
            private static readonly IList<MethodInfo> _actions = new List<MethodInfo>();
            private static readonly IList<MethodInfo> _funcs = new List<MethodInfo>();
            
            static Delegator() {
                var methods = typeof(Delegator).GetMethods(BindingFlags.Instance | BindingFlags.Public);
                _actions = methods.Where(m => m.Name.StartsWith("Action")).OrderBy(m => m.GetParameters().Length).ToArray();
                _funcs = methods.Where(m => m.Name.StartsWith("Func")).OrderBy(m => m.GetParameters().Length).ToArray();
            }
            
            public static MethodInfo GetMethod(InterpretedMethod method) {
                var parameters = method.GetParameters();
                var types = parameters.Select(p => Erase(p.ParameterType)).ToList();
                if (method.ReturnType == typeof(void))
                    return _actions[parameters.Length].MakeGenericMethod(types.ToArray());

                types.Add(Erase(method.ReturnType));
                return _funcs[parameters.Length].MakeGenericMethod(types.ToArray());
            }

            private static Type Erase(Type type) {
                return (type is InterpretedType) ? typeof(CilinObject) : type;
            }

            public void Action() {}
            public void Action<TArg1>(TArg1 arg1) {}
            public TResult Func<TResult>() { return default(TResult); }
            public TResult Func<TArg1, TResult>(TArg1 arg1) { return default(TResult); }
            public TResult Func<TArg1, TArg2, TResult>(TArg1 arg1, TArg2 arg2) { return default(TResult); }
        }
    }
}
