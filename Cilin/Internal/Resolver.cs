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
        private readonly IDictionary<IMemberDefinition, MemberInfo> _memberDefinitionCache = new Dictionary<IMemberDefinition, MemberInfo>();
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

            var parameter = reference as GenericParameter;
            if (reference is GenericParameter)
                return genericScope?.Resolve(parameter) ?? new Reflection.GenericParameterType(reference.Name);

            var definition = reference.Resolve();
            var definitionType = TypeByDefinition(definition);

            if (reference.IsArray) {
                throw new NotImplementedException();
                //var elementType = Type(reference.GetElementType(), genericScope);
                //if (TypeSupport.IsRuntime(elementType))
                //    return elementType.MakeArrayType();

                //return new ErasedWrapperType(
                //    typeof(CilinObject).MakeArrayType(),
                //    NewIntepretedArrayType(reference.GetElementType(), elementType, genericScope)
                //);
            }

            //genericScope = declaringType != null ? WithTypeScope(genericScope, definition.DeclaringType, declaringType) : genericScope;

            var declaringType = reference.DeclaringType != null ? Type(reference.DeclaringType, genericScope) : null;
            if (reference.IsGenericInstance || HasGenericPath(declaringType))
                return GenericPathType(declaringType, definitionType, definition, reference, genericScope);

            return definitionType;
        }

        private Type TypeByDefinition(TypeDefinition definition) {
            MemberInfo cached;
            if (_memberDefinitionCache.TryGetValue(definition, out cached))
                return (Type)cached;

            if (ShouldBeRuntime(definition)) {
                var fullName = TypeSupport.GetFullName(definition);
                try {
                    return System.Type.GetType(fullName + ", " + definition.Module.Assembly.Name, true);
                }
                catch (Exception ex) {
                    throw new Exception($"Failed to resolve type '{fullName}': {ex.Message}.", ex);
                }
            }

            var declaringType = definition.DeclaringType != null ? TypeByDefinition(definition.DeclaringType) : null;
            var type = NewInterpretedDefinitionType(declaringType, definition);
            _memberDefinitionCache.Add(definition, type);
            return type;
        }

        private Type GenericPathType(Type declaringType, Type definitionType, TypeDefinition definition, TypeReference reference, GenericScope genericScope) {
            var generic = reference as GenericInstanceType;
            var arguments = generic != null ? new Type[generic.GenericArguments.Count] : System.Type.EmptyTypes;
            var allArgumentsAreRuntime = true;
            if (generic != null) {
                for (var i = 0; i < generic.GenericArguments.Count; i++) {
                    var resolved = Type(generic.GenericArguments[i], genericScope);
                    allArgumentsAreRuntime = allArgumentsAreRuntime && TypeSupport.IsRuntime(resolved);
                    arguments[i] = resolved;
                }
            }

            var cacheKey = new GenericTypeKey(declaringType, definitionType, arguments);
            Type cached;
            if (_genericTypeCache.TryGetValue(cacheKey, out cached))
                return cached;

            var resultType = GenericPathTypeUncached(declaringType, definitionType, definition, arguments, allArgumentsAreRuntime, genericScope);
            _genericTypeCache.Add(cacheKey, resultType);
            return resultType;
        }

        private Type GenericPathTypeUncached(Type declaringType, Type definitionType, TypeDefinition definition, Type[] arguments, bool allArgumentsAreRuntime, GenericScope genericScope) {            
            if (!TypeSupport.IsRuntime(definitionType))
                return NewInterpretedGenericPathType(declaringType, definitionType, definition, arguments, genericScope);

            if (arguments.Length == 0)
                return declaringType.GetNestedType(definitionType.Name);

            if (allArgumentsAreRuntime)
                return definitionType.MakeGenericType(arguments);

            var erased = new Type[arguments.Length];
            for (var i = 0; i < arguments.Length; i++) {
                erased[i] = TypeSupport.IsRuntime(arguments[i]) ? arguments[i] : typeof(CilinObject);
            }
            var erasedFull = definitionType.MakeGenericType(erased);
            return new ErasedWrapperType(erasedFull, NewInterpretedGenericPathType(declaringType, definitionType, definition, arguments, genericScope));
        }

        private InterpretedType NewInterpretedDefinitionType(Type declaringType, TypeDefinition definition) {
            var lazyBaseType = new Lazy<Type>(() => Type(definition.BaseType, null));
            return new InterpretedDefinitionType(
                definition.Name,
                definition.Namespace,
                Assembly(definition.Module.Assembly),
                declaringType,
                lazyBaseType,
                LazyInterfacesOf(definition, lazyBaseType),
                null,
                t => LazyMembersOf(t, definition),
                (System.Reflection.TypeAttributes)definition.Attributes,
                definition.HasGenericParameters
                    ? definition.GenericParameters.Select(p => Type(p, null)).ToArray()
                    : System.Type.EmptyTypes
            );
        }

        private InterpretedType NewInterpretedGenericPathType(Type declaringType, Type definitionType, TypeDefinition definition, Type[] genericArguments, GenericScope genericScope) {
            genericScope = genericArguments.Length > 0 ? new GenericScope(definition.GenericParameters, genericArguments, genericScope) : genericScope;

            var lazyBaseType = new Lazy<Type>(() => Type(definition.BaseType, genericScope));
            return new InterpretedGenericPathType(
                definitionType,
                lazyBaseType,
                LazyInterfacesOf(definition, lazyBaseType, genericScope),
                t => LazyMembersOf(t, definition, genericScope),
                genericArguments
            );
        }

        private InterpretedType NewIntepretedArrayType(TypeReference elementTypeReference, Type elementType, GenericScope genericScope) {
            return new InterpretedDefinitionType(
                elementType.Name + "[]",
                elementType.Namespace,
                elementType.Assembly,
                null,
                LazyArrayType,
                new Lazy<Type[]>(() => TypeSupport.GetArrayInterfaces(elementTypeReference).Select(i => Type(i, genericScope)).ToArray()),
                null,
                t => Empty<LazyMember>.Array,
                typeof(Array).Attributes,
                null
            );
        }

        private Lazy<Type[]> LazyInterfacesOf(TypeDefinition definition, Lazy<Type> lazyBaseType, GenericScope genericScope = null) {
            return new Lazy<Type[]>(() => {
                var baseInterfaces = lazyBaseType.Value.GetInterfaces();
                if (baseInterfaces.Length == 0 && definition.Interfaces.Count == 0)
                    return System.Type.EmptyTypes;

                var interfaces = new Type[baseInterfaces.Length + definition.Interfaces.Count];
                Array.Copy(baseInterfaces, interfaces, baseInterfaces.Length);
                for (var i = 0; i < definition.Interfaces.Count; i++) {
                    interfaces[baseInterfaces.Length + i] = Type(definition.Interfaces[i], genericScope);
                }
                return interfaces;
            });
        }

        private IReadOnlyCollection<LazyMember> LazyMembersOf(Type type, TypeDefinition definition, GenericScope genericScope = null) {
            var list = new List<LazyMember>();
            foreach (var method in definition.Methods) {
                if (method.IsConstructor) {
                    list.Add(new LazyMember<ConstructorInfo>(method.Name, method.IsStatic, method.IsPublic, () => (ConstructorInfo)Method(type, method, genericScope)));
                }
                else {
                    list.Add(new LazyMember<MethodInfo>(method.Name, method.IsStatic, method.IsPublic, () => (MethodInfo)Method(type, method, genericScope)));
                }
            }

            foreach (var field in definition.Fields) {
                list.Add(new LazyMember<FieldInfo>(field.Name, field.IsStatic, field.IsPublic, () => Field(field, genericScope)));
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

            var definition = reference.Resolve();
            var declaringType = Type(reference.DeclaringType, genericScope);

            genericScope = WithTypeScope(genericScope, definition.DeclaringType, declaringType);

            var interpretedType = declaringType as InterpretedType;
            if (ShouldBeRuntime(definition) && interpretedType == null) {
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
            var declaringType = Type(reference.DeclaringType, genericScope);
            return Method(declaringType, reference, genericScope);
        }

        private MethodBase Method(Type declaringType, MethodReference reference, GenericScope genericScope) {
            var definition = reference.Resolve();
            if (definition == null)
                throw new Exception($"Failed to resolve definition for method {reference}.");

            genericScope = WithTypeScope(genericScope, definition.DeclaringType, declaringType);

            var interpretedType = declaringType as InterpretedType;
            if (ShouldBeRuntime(definition) && interpretedType == null) {
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

            var arguments = type.GetGenericArguments();
            if (arguments.Length == 0)
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

        private bool HasGenericPath(Type type) {
            return type != null
                && (type.IsConstructedGenericType || HasGenericPath(type.DeclaringType));
        }

        private bool ShouldBeRuntime(AssemblyDefinition assembly) {
            return assembly.Name.Name == "mscorlib";
        }

        private bool ShouldBeRuntime(TypeDefinition definition) {
            return ShouldBeRuntime<TypeDefinition>(definition)
                && !IsDelegate(definition);
        }

        private bool ShouldBeRuntime<TDefinition>(TDefinition definition)
            where TDefinition : MemberReference, IMemberDefinition
        {
            return ShouldBeRuntime(definition.Module.Assembly);
        }

        private bool IsDelegate(TypeDefinition definition) {
            return definition.BaseType?.FullName == "System.MulticastDelegate"
                || definition.BaseType?.FullName == "System.Delegate";
        }

        private bool IsDelegate(Type declaringType) => declaringType.IsSubclassOf<Delegate>();

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