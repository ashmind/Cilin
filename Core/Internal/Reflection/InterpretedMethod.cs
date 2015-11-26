using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using MethodAttributes = System.Reflection.MethodAttributes;
using MethodImplAttributes = System.Reflection.MethodImplAttributes;

namespace Cilin.Core.Internal.Reflection {
    public class InterpretedMethod : MethodInfo, IInterpretedMethodBase {
        private readonly Module _module;
        private readonly InterpretedType _declaringType;
        private readonly string _name;
        private readonly Type _returnType;
        private readonly ParameterInfo[] _parameters;
        private readonly Lazy<MethodInfo[]> _explicitOverrides;
        private readonly MethodAttributes _attributes;
        private readonly GenericDetails _generic;
        private readonly MethodInvoker _invoker;

        private MethodInfo _baseDefinition;

        public InterpretedMethod(
            Module module,
            InterpretedType declaringType,
            string name,
            Type returnType,
            ParameterInfo[] parameters,
            Lazy<MethodInfo[]> explicitOverrides,
            MethodAttributes attributes,
            GenericDetails generic,
            MethodInvoker invoker,
            object invokableDefinition
        ) {
            _module = module;
            _declaringType = declaringType;
            _name = name;
            _returnType = returnType;
            _parameters = parameters;
            _explicitOverrides = explicitOverrides;
            _attributes = attributes;
            _generic = generic;
            _invoker = invoker;
            InvokableDefinition = invokableDefinition;
        }

        public override MethodAttributes Attributes => _attributes;
        public override Type DeclaringType => _declaringType;

        public override RuntimeMethodHandle MethodHandle {
            get { throw new NotImplementedException(); }
        }

        public override Module Module => _module;
        public override string Name => _name;

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
            if (!IsVirtual || IsAbstract || IsStatic || DeclaringType == null || DeclaringType.IsInterface)
                return this;

            if (_baseDefinition != null)
                return _baseDefinition;

            // not fully correct
            _baseDefinition = FindBaseMethod(this, DeclaringType.BaseType) ?? this;
            return _baseDefinition;
        }

        public MethodInfo[] GetExplicitOverrides() => _explicitOverrides.Value;

        public override object[] GetCustomAttributes(bool inherit) {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }

        public override Type[] GetGenericArguments() => _generic?.ParametersOrArguments ?? Empty<Type>.Array;

        public override MethodImplAttributes GetMethodImplementationFlags() {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters() => _parameters;

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            return _invoker.Invoke(this, obj, parameters, null, invokeAttr, binder, culture);
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }

        public override Type ReturnType => _returnType;

        public object InvokableDefinition { get; }

        InterpretedType IInterpretedMethodBase.DeclaringType => _declaringType;

        public override string ToString() => Name;

        private static MethodInfo FindBaseMethod(MethodInfo method, Type baseType) {
            var overrides = (method as InterpretedMethod)?.GetExplicitOverrides();
            if (overrides != null) {
                foreach (var @override in overrides) {
                    if (@override.DeclaringType == baseType)
                        return @override;
                }
            }

            var nameMatches = baseType.FindMembers(MemberTypes.Method, BindingFlags.Default, Type.FilterName, method.Name);
            if (nameMatches.Length > 0)
                return FindMethodBySignature(method, nameMatches);

            return null;
        }

        public static MethodInfo FindTargetMethodRaw(MethodInfo method, Type targetType) {
            var nameMatches = targetType.FindMembers(MemberTypes.Method, BindingFlags.Default, Type.FilterName, method.Name);
            MethodInfo match = null;
            if (nameMatches.Length > 0)
                match = FindMethodBySignature(method, nameMatches);

            match = match ?? FindTargetMethodByExplicitOverrides(method, targetType);
            if (match == null)
                throw new MissingMethodException($"Could not find method matching {method} on type {targetType}.");

            return match;
        }

        private static MethodInfo FindTargetMethodByExplicitOverrides(MethodInfo method, Type targetType) {
            if (TypeSupport.IsRuntime(targetType))
                throw new NotImplementedException();

            foreach (var candidate in targetType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)) {
                var overrides = ((InterpretedMethod)candidate).GetExplicitOverrides();
                if (overrides.Contains(method))
                    return candidate;
            }

            return null;
        }

        private static MethodInfo FindMethodBySignature(MethodInfo method, MemberInfo[] nameMatches) {
            if (nameMatches.Length == 1)
                return (MethodInfo)nameMatches[0];

            MethodInfo match = null;
            foreach (MethodInfo candidate in nameMatches) {
                if (MethodMatches(method, candidate)) {
                    if (match != null)
                        throw new AmbiguousMatchException();

                    match = candidate;
                }
            }

            return match;
        }

        private static bool MethodMatches(MethodInfo method, MethodInfo candidate) {
            if (method.Name != candidate.Name)
                return false;

            if (method.IsGenericMethod) {
                if (!candidate.IsGenericMethod)
                    return false;

                if (!candidate.GetGenericArguments().SequenceEqual(method.GetGenericArguments()))
                    return false;
            }

            if (candidate.IsGenericMethod)
                return false;

            var methodParameters = method.GetParameters();
            var candidateParameters = candidate.GetParameters();
            if (methodParameters.Length != candidateParameters.Length)
                return false;

            for (var i = 0; i < methodParameters.Length; i++) {
                if (methodParameters[i].ParameterType != candidateParameters[i].ParameterType)
                    return false;
            }

            return true;
        }
    }
}
