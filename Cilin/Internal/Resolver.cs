using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AshMind.Extensions;
using Cilin.Internal.Reflection;
using Cilin.Internal.State;
using Mono.Cecil;

namespace Cilin.Internal {
    public class Resolver {
        private static readonly Lazy<Type> LazyArrayType = new Lazy<Type>(() => typeof(Array));
        private static readonly Lazy<Type[]> LazyEmptyTypes = new Lazy<Type[]>(() => System.Type.EmptyTypes);
        private static readonly ParameterInfo[] DelegateParameters = new[] {
            new InterpretedParameter(typeof(object)),
            new InterpretedParameter(typeof(MethodPointerWrapper))
        };

        private static readonly MemberReferenceEqualityComparer ReferenceComparer = new MemberReferenceEqualityComparer();

        private readonly IDictionary<string, Assembly> _assemblyCache = new Dictionary<string, Assembly>();
        private readonly IDictionary<MemberReference, MemberInfo> _memberInfoCache = new Dictionary<MemberReference, MemberInfo>(ReferenceComparer);
        private readonly IDictionary<GenericTypeKey, Type> _genericTypeCache = new Dictionary<GenericTypeKey, Type>();

        private readonly MethodInvoker _invoker;

        public Resolver(MethodInvoker invoker) {
            _invoker = invoker;
        }

        public Assembly Assembly(AssemblyDefinition assembly) {
            Argument.NotNull(nameof(assembly), assembly);
            return _assemblyCache.GetOrAdd(assembly.Name.Name, _ => {
                if (ShouldBeRuntime(assembly))
                    return typeof(object).Assembly; // TODO

                return new InterpretedAssembly(assembly.FullName);
            });
        }

        public Type Type(TypeReference reference, GenericScope genericScope) {
            Argument.NotNull(nameof(reference), reference);

            if (reference.IsGenericParameter)
                return TypeUncached(reference, genericScope);

            MemberInfo cached;
            if (_memberInfoCache.TryGetValue(reference, out cached))
                return (Type)cached;

            var type = TypeUncached(reference, genericScope);
            _memberInfoCache.Add(reference, type);
            return type;
        }

        private Type TypeUncached(TypeReference reference, GenericScope genericScope) {
            var parameter = reference as GenericParameter;
            if (reference is GenericParameter)
                return genericScope.Resolve(parameter) ?? new Reflection.GenericParameterType(reference.Name);

            var definition = reference.Resolve();
            if (reference.IsArray) {
                var elementType = Type(reference.GetElementType(), genericScope);
                if (IsRuntime(elementType))
                    return elementType.MakeArrayType();

                return new ErasedWrapperType(
                    typeof(CilinObject).MakeArrayType(),
                    NewIntepretedArrayType(reference.GetElementType(), elementType, genericScope)
                );
            }

            var declaringType = definition.DeclaringType != null ? Type(definition.DeclaringType, genericScope) : null;
            genericScope = declaringType != null ? WithTypeScope(genericScope, definition.DeclaringType, declaringType) : genericScope;

            var generic = reference as GenericInstanceType;
            if (generic != null)
                return GenericTypeSeparatelyCached(declaringType, generic, definition, genericScope);

            if (ShouldBeRuntime(definition, reference)) {
                var fullName = TypeSupport.GetFullName(reference);
                try {
                    return System.Type.GetType(fullName + ", " + definition.Module.Assembly.Name, true);
                }
                catch (Exception ex) {
                    throw new Exception($"Failed to resolve type '{fullName}': {ex.Message}.", ex);
                }
            }

            return NewInterpretedType(declaringType, definition, reference, genericScope);
        }

        private Type GenericTypeSeparatelyCached(Type declaringType, GenericInstanceType reference, TypeDefinition definition, GenericScope genericScope) {
            var genericDefinition = Type(definition, genericScope);

            var arguments = new Type[reference.GenericArguments.Count];
            bool allArgumentsAreRuntime = true;
            for (var i = 0; i < reference.GenericArguments.Count; i++) {
                var resolved = Type(reference.GenericArguments[i], genericScope);
                allArgumentsAreRuntime = allArgumentsAreRuntime && IsRuntime(resolved);
                arguments[i] = resolved;
            }

            var cacheKey = new GenericTypeKey(declaringType, genericDefinition, arguments);
            Type cached;
            if (_genericTypeCache.TryGetValue(cacheKey, out cached))
                return cached;

            if (IsRuntime(genericDefinition)) {
                if (allArgumentsAreRuntime)
                    return genericDefinition.MakeGenericType(arguments);

                var erased = new Type[arguments.Length];
                for (var i = 0; i < arguments.Length; i++) {
                    erased[i] = IsRuntime(arguments[i]) ? arguments[i] : typeof(CilinObject);
                }
                var erasedFull = genericDefinition.MakeGenericType(erased);
                var wrappedType = new ErasedWrapperType(erasedFull, NewInterpretedType(declaringType, definition, reference, genericScope, arguments));
                _genericTypeCache.Add(cacheKey, wrappedType);
                return wrappedType;
            }

            var type = NewInterpretedType(declaringType, definition, reference, genericScope, arguments);
            _genericTypeCache.Add(cacheKey, type);
            return type;
        }

        private InterpretedType NewInterpretedType(Type declaringType, TypeDefinition definition, TypeReference reference, GenericScope genericScope, Type[] genericArguments = null) {
            var generic = GenericDetails(definition, genericArguments, genericScope);
            if (generic?.IsConstructed ?? false)
                genericScope = new GenericScope(definition.GenericParameters, generic.ParametersOrArguments, genericScope);

            var lazyBaseType = new Lazy<Type>(() => Type(definition.BaseType, genericScope));
            return new InterpretedType(
                definition.Name,
                definition.Namespace,
                Assembly(definition.Module.Assembly),
                declaringType,
                lazyBaseType,
                new Lazy<Type[]>(() => {
                    var baseInterfaces = lazyBaseType.Value.GetInterfaces();
                    if (baseInterfaces.Length == 0 && definition.Interfaces.Count == 0)
                        return System.Type.EmptyTypes;

                    var interfaces = new Type[baseInterfaces.Length + definition.Interfaces.Count];
                    Array.Copy(baseInterfaces, interfaces, baseInterfaces.Length);
                    for (var i = 0; i < definition.Interfaces.Count; i++) {
                        interfaces[baseInterfaces.Length + i] = Type(definition.Interfaces[i], genericScope);
                    }
                    return interfaces;
                }),
                null,
                LazyMembersOf(definition, reference, genericScope),
                (System.Reflection.TypeAttributes)definition.Attributes,
                generic
            );
        }

        private InterpretedType NewIntepretedArrayType(TypeReference elementTypeReference, Type elementType, GenericScope genericScope) {
            return new InterpretedType(
                elementType.Name + "[]",
                elementType.Namespace,
                elementType.Assembly,
                null,
                LazyArrayType,
                new Lazy<Type[]>(() => TypeSupport.GetArrayInterfaces(elementTypeReference).Select(i => Type(i, genericScope)).ToArray()),
                null,
                new LazyMember[0],
                typeof(Array).Attributes,
                null
            );
        }

        private IReadOnlyCollection<LazyMember> LazyMembersOf(TypeDefinition definition, TypeReference reference, GenericScope genericScope) {
            var list = new List<LazyMember>();
            foreach (var method in definition.Methods) {
                if (method.IsConstructor) {
                    list.Add(new LazyMember<ConstructorInfo>(method.Name, method.IsStatic, () => (ConstructorInfo)Method(ToReference(method, reference), genericScope)));
                }
                else {
                    list.Add(new LazyMember<MethodInfo>(method.Name, method.IsStatic, () => (MethodInfo)Method(ToReference(method, reference), genericScope)));
                }
            }

            foreach (var field in definition.Fields) {
                list.Add(new LazyMember<FieldInfo>(field.Name, field.IsStatic, () => Field(ToReference(field, reference), genericScope)));
            }

            return list;
        }

        private MethodReference ToReference(MethodDefinition definition, TypeReference declaringType) {
            if (!declaringType.IsGenericInstance)
                return definition;

            var reference = new MethodReference(definition.Name, definition.ReturnType, declaringType) {
                CallingConvention = definition.CallingConvention,
                ExplicitThis = definition.ExplicitThis,
                HasThis = definition.HasThis,
                MetadataToken = definition.MetadataToken,
                MethodReturnType = definition.MethodReturnType
            };
            reference.GenericParameters.AddRange(definition.GenericParameters);
            reference.Parameters.AddRange(definition.Parameters);

            return reference;
        }

        private FieldReference ToReference(FieldDefinition definition, TypeReference declaringType) {
            if (!declaringType.IsGenericInstance)
                return definition;

            var reference = new FieldReference(definition.Name, definition.FieldType, declaringType) {
                MetadataToken = definition.MetadataToken
            };
            return reference;
        }

        public FieldInfo Field(FieldReference reference, GenericScope genericScope) {
            Argument.NotNull(nameof(reference), reference);

            MemberInfo cached;
            if (_memberInfoCache.TryGetValue(reference, out cached))
                return (FieldInfo)cached;

            var field = FieldUncached(reference, genericScope);
            _memberInfoCache.Add(reference, field);
            return field;
        }

        private FieldInfo FieldUncached(FieldReference reference, GenericScope genericScope = null) {
            var definition = reference.Resolve();
            var declaringType = Type(reference.DeclaringType, genericScope);

            genericScope = WithTypeScope(genericScope, definition.DeclaringType, declaringType);

            var interpretedType = declaringType as InterpretedType;
            if (ShouldBeRuntime(definition, reference) && interpretedType == null) {
                var token = reference.MetadataToken.ToInt32();
                return (FieldInfo)declaringType
                    .GetMembers()
                    .First(m => m.MetadataToken == token);
            }
            if (interpretedType == null)
                throw InterpretedMemberInNonInterpretedType(reference, declaringType);

            return new InterpretedField(
                interpretedType,
                definition.Name,
                Type(reference.FieldType, genericScope),
                (System.Reflection.FieldAttributes)definition.Attributes
            );
        }
        
        public MethodBase Method(MethodReference reference, GenericScope genericScope) {
            Argument.NotNull(nameof(reference), reference);

            MemberInfo cached;
            if (_memberInfoCache.TryGetValue(reference, out cached))
                return (MethodBase)cached;

            var method = MethodUncached(reference, genericScope);
            _memberInfoCache.Add(reference, method);
            return method;
        }

        private MethodBase MethodUncached(MethodReference reference, GenericScope genericScope) {
            var definition = reference.Resolve();
            if (definition == null)
                throw new Exception($"Failed to resolve definition for method {reference}.");

            var declaringType = Type(reference.DeclaringType, genericScope);
            genericScope = WithTypeScope(genericScope, definition.DeclaringType, declaringType);

            var interpretedType = declaringType as InterpretedType;
            if (ShouldBeRuntime(definition, reference) && interpretedType == null) {
                var token = definition.MetadataToken.ToInt32();
                return (MethodBase)declaringType
                    .GetMembers()
                    .First(m => m.MetadataToken == token);
            }
            if (interpretedType == null)
                throw InterpretedMemberInNonInterpretedType(reference, declaringType);

            var typeArguments = Empty<Type>.Array;
            var generic = reference as GenericInstanceMethod;
            if (generic != null) {
                typeArguments = generic.GenericArguments.Select(a => Type(a, genericScope)).ToArray();
                genericScope = new GenericScope(
                    definition.GenericParameters,
                    generic.GenericArguments.Select(a => Type(a, genericScope)),
                    genericScope
                );
            }

            var special = SpecialMethodUncached(interpretedType, reference, definition, genericScope, typeArguments);
            if (special != null)
                return special;

            return NewInterpretedMethod(interpretedType, reference, definition, genericScope, typeArguments);
        }

        private MethodBase SpecialMethodUncached(
            InterpretedType declaringType,
            MethodReference reference,
            MethodDefinition definition,
            GenericScope genericScope,
            Type[] typeArguments
        ) {
            if (IsDelegate(declaringType) && definition.IsConstructor) {
                if (definition.IsConstructor) {
                    return new InterpretedConstructor(
                        declaringType,
                        definition.Name,
                        DelegateParameters,
                        0,
                        args => new CilinDelegate(args[0], (MethodPointerWrapper)args[1], declaringType),
                        _invoker,
                        definition
                    );
                }
            }

            return null;
        }

        private MethodBase NewInterpretedMethod(
            InterpretedType declaringType,
            MethodReference reference,
            MethodDefinition definition,
            GenericScope genericScope,
            Type[] genericArguments = null
        ) {
            var parameters = reference.Parameters.Select(p => Parameter(p, genericScope)).ToArray();
            var attributes = (System.Reflection.MethodAttributes)definition.Attributes;

            if (definition.IsConstructor)
                return new InterpretedConstructor(declaringType,  definition.Name, parameters, attributes, _ => new CilinObject(declaringType), _invoker, definition);

            var returnType = Type(reference.ReturnType, genericScope);
            return new InterpretedMethod(
                declaringType,
                reference.Name,
                returnType,
                parameters,
                attributes,
                GenericDetails(definition, genericArguments, genericScope),
                _invoker,
                definition
            );
        }

        private ParameterInfo Parameter(ParameterDefinition definition, GenericScope genericScope) {
            return new InterpretedParameter(Type(definition.ParameterType, genericScope));
        }

        private GenericDetails GenericDetails(IGenericParameterProvider definition, Type[] genericArguments, GenericScope genericScope) {
            var genericParameters = definition.GenericParameters.Count > 0
                ? definition.GenericParameters.Select(p => Type(p, genericScope)).ToArray()
                : null;

            return genericParameters != null || genericArguments != null
                ? new GenericDetails(genericArguments != null, genericParameters != null, genericArguments ?? genericParameters)
                : null;
        }

        private GenericScope WithTypeScope(GenericScope genericScope, TypeDefinition definition, Type type) {
            if (!type.IsConstructedGenericType)
                return genericScope;

            return new GenericScope(
                definition.GenericParameters,
                type.GetGenericArguments(),
                genericScope
            );
        }

        private Exception InterpretedMemberInNonInterpretedType(MemberReference reference, Type type) {
            return new Exception($"{reference} belongs to a non-interpreted type {type} and has to be non-intepreted.");
        }

        private bool ShouldBeRuntime(AssemblyDefinition assembly) {
            return assembly.Name.Name == "mscorlib";
        }

        private bool ShouldBeRuntime(TypeDefinition definition, TypeReference reference) {
            return ShouldBeRuntime<TypeDefinition, TypeReference>(definition, reference)
                && !IsDelegate(definition);
        }

        private bool ShouldBeRuntime<TDefinition, TReference>(TDefinition definition, TReference reference)
            where TDefinition : TReference, IMemberDefinition
            where TReference : MemberReference
        {
            return ShouldBeRuntime(definition.Module.Assembly);
        }

        private bool IsRuntime(Type type) {
            return !(type is INonRuntimeType);
        }

        private bool IsDelegate(TypeDefinition definition) {
            return definition.BaseType?.FullName == "System.MulticastDelegate"
                || definition.BaseType?.FullName == "System.Delegate";
        }

        private bool IsDelegate(Type declaringType) {
            return declaringType.IsSubclassOf<Delegate>();
        }

        private struct GenericTypeKey : IEquatable<GenericTypeKey> {
            public Type DeclaringType { get; }
            public Type GenericDefinition { get; }
            public Type[] GenericArguments { get; }

            public GenericTypeKey(Type declaringType, Type genericDefinition, Type[] genericArguments) {
                DeclaringType = declaringType;
                GenericDefinition = genericDefinition;
                GenericArguments = genericArguments;
            }

            public bool Equals(GenericTypeKey other) {
                if (DeclaringType != other.DeclaringType || GenericDefinition != other.GenericDefinition)
                    return false;

                if (GenericArguments.Length != other.GenericArguments.Length)
                    return false;

                for (var i = 0; i < GenericArguments.Length; i++) {
                    if (GenericArguments[i] != other.GenericArguments[i])
                        return false;
                }

                return true;
            }

            public override bool Equals(object obj) {
                if (!(obj is GenericTypeKey))
                    return false;

                return Equals((GenericTypeKey)obj);
            }

            public override int GetHashCode() {
                var hashCode = (DeclaringType?.GetHashCode() ?? 0) ^ GenericDefinition.GetHashCode();
                for (var i = 0; i < GenericArguments.Length; i++) {
                    hashCode ^= GenericArguments[i].GetHashCode();
                }
                return hashCode;
            }
        }
    }
}