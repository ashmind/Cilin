using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;
using AshMind.Extensions;
using Cilin.Core.Internal.Reflection;
using Cilin.Core.Internal.State;

namespace Cilin.Core.Internal {
    public class Resolver {
        private static readonly Lazy<Type[]> LazyEmptyTypes = new Lazy<Type[]>(() => System.Type.EmptyTypes, LazyThreadSafetyMode.PublicationOnly);
        private static readonly Lazy<Type> LazyNullType = new Lazy<Type>(() => null, LazyThreadSafetyMode.PublicationOnly);
        private static readonly Lazy<MethodInfo[]> LazyEmptyMethods = new Lazy<MethodInfo[]>(() => Empty<MethodInfo>.Array, LazyThreadSafetyMode.PublicationOnly);
        private static readonly ParameterInfo[] DelegateParameters = new[] {
            new InterpretedParameter(typeof(object)),
            new InterpretedParameter(typeof(NonRuntimeMethodPointer))
        };

        private static readonly MemberReferenceEqualityComparer ReferenceComparer = new MemberReferenceEqualityComparer();

        private readonly IDictionary<string, Assembly> _assemblyCache = new Dictionary<string, Assembly>();
        private readonly IDictionary<string, Module> _moduleCache = new Dictionary<string, Module>();
        private readonly IDictionary<IMemberDefinition, MemberInfo> _memberDefinitionCache = new Dictionary<IMemberDefinition, MemberInfo>();
        private readonly IDictionary<GenericTypeKey, Type> _genericTypeCache = new Dictionary<GenericTypeKey, Type>();
        private readonly IDictionary<MethodKey, MethodBase> _methodCache = new Dictionary<MethodKey, MethodBase>();
        private readonly IDictionary<Type, Type> _arrayTypeCache = new Dictionary<Type, Type>();

        private readonly MethodInvoker _invoker;
        private readonly IDictionary<AssemblyDefinition, string> _assemblyPathMap;

        public Resolver(MethodInvoker invoker, IDictionary<AssemblyDefinition, string> assemblyPathMap) {
            _invoker = invoker;
            _assemblyPathMap = assemblyPathMap;
        }

        public Assembly Assembly(AssemblyDefinition assembly) {
            Argument.NotNull(nameof(assembly), assembly);
            return _assemblyCache.GetOrAdd(assembly.Name.Name, _ => {
                if (ShouldBeRuntime(assembly))
                    return typeof(object).Assembly; // TODO

                return new InterpretedAssembly(assembly.FullName, _assemblyPathMap.GetValueOrDefault(assembly));
            });
        }

        public Module Module(ModuleDefinition module) {
            Argument.NotNull(nameof(module), module);
            return _moduleCache.GetOrAdd(module.FullyQualifiedName, _ => {
                if (ShouldBeRuntime(module))
                    return typeof(object).Module; // TODO

                return new InterpretedModule((InterpretedAssembly)Assembly(module.Assembly));
            });
        }

        public Type Type(TypeReference reference, GenericScope genericScope) {
            Argument.NotNull(nameof(reference), reference);

            var parameter = reference as GenericParameter;
            if (reference is GenericParameter)
                return genericScope?.Resolve(parameter) ?? new Reflection.GenericParameterType(reference.Name);

            if (reference.IsArray) {
                var elementType = Type(reference.GetElementType(), genericScope);
                Type cached;
                if (_arrayTypeCache.TryGetValue(elementType, out cached))
                    return cached;

                var array = ArrayTypeUncached(elementType, genericScope);
                _arrayTypeCache.Add(elementType, array);
                return array;
            }

            var definition = reference.Resolve();
            var definitionType = TypeByDefinition(definition);

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

        private Type ArrayTypeUncached(Type elementType, GenericScope genericScope) {
            if (TypeSupport.IsRuntime(elementType))
                return elementType.MakeArrayType();

            return /*new ErasedWrapperType(
                typeof(CilinObject).MakeArrayType(),*/
                NewIntepretedArrayType((NonRuntimeType)elementType, genericScope)
            /*)*/;
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
            return GenericPathType(declaringType, definitionType, definition, arguments, allArgumentsAreRuntime, genericScope);
        }

        private Type GenericPathType(Type declaringType, Type definitionType, TypeDefinition definition, Type[] arguments, bool allArgumentsAreRuntime, GenericScope genericScope) {
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

            return NewInterpretedGenericPathType(declaringType, definitionType, definition, arguments, genericScope);

            //var erased = new Type[arguments.Length];
            //for (var i = 0; i < arguments.Length; i++) {
            //    erased[i] = TypeSupport.IsRuntime(arguments[i]) ? arguments[i] : typeof(CilinObject);
            //}
            //var erasedFull = definitionType.MakeGenericType(erased);
            //return new ErasedWrapperType(erasedFull, NewInterpretedGenericPathType(declaringType, definitionType, definition, arguments, genericScope));
        }

        private InterpretedType NewInterpretedDefinitionType(Type declaringType, TypeDefinition definition) {
            var lazyBaseType = definition.BaseType != null ? new Lazy<Type>(() => Type(definition.BaseType, GenericScope.None)) : LazyNullType;
            return new InterpretedDefinitionType(
                definition.Name,
                definition.Namespace,
                Assembly(definition.Module.Assembly),
                declaringType,
                lazyBaseType,
                LazyInterfacesOf(definition, lazyBaseType, GenericScope.None),
                t => LazyMembersOf(t, definition, GenericScope.None),
                (System.Reflection.TypeAttributes)definition.Attributes,
                definition.HasGenericParameters
                    ? definition.GenericParameters.Select(p => Type(p, GenericScope.None)).ToArray()
                    : System.Type.EmptyTypes
            );
        }

        private InterpretedType NewInterpretedGenericPathType(Type declaringType, Type definitionType, TypeDefinition definition, Type[] genericArguments, GenericScope genericScope) {
            genericScope = genericScope.With(definition.GenericParameters, genericArguments);

            var lazyBaseType = definition.BaseType != null ? new Lazy<Type>(() => Type(definition.BaseType, genericScope)) : LazyNullType;
            return new InterpretedGenericPathType(
                definitionType,
                lazyBaseType,
                LazyInterfacesOf(definition, lazyBaseType, genericScope),
                t => LazyMembersOf(t, definition, genericScope),
                genericArguments
            );
        }

        private InterpretedType NewIntepretedArrayType(NonRuntimeType elementType, GenericScope genericScope) {
            return new InterpretedArrayType(
                elementType,
                new Lazy<Type[]>(() => GetArrayInterfaces(elementType, genericScope)),
                t => Empty<ILazyMember<MemberInfo>>.Array
            );
        }

        public Type[] GetArrayInterfaces(NonRuntimeType elementType, GenericScope genericScope) {
            var elementTypeArray = new[] { elementType };
            return new Type[] {
                typeof(IEnumerable),
                typeof(ICollection),
                typeof(IList),
                GenericPathType(null, typeof(IEnumerable<>), TypeSupport.Definitions.IEnumerableOfT, elementTypeArray, false, genericScope),
                GenericPathType(null, typeof(IReadOnlyCollection<>), TypeSupport.Definitions.IReadOnlyCollectionOfT, elementTypeArray, false, genericScope),
                GenericPathType(null, typeof(ICollection<>), TypeSupport.Definitions.ICollectionOfT, elementTypeArray, false, genericScope),
                GenericPathType(null, typeof(IReadOnlyList<>), TypeSupport.Definitions.IReadOnlyListOfT, elementTypeArray, false, genericScope),
                GenericPathType(null, typeof(IList<>), TypeSupport.Definitions.IListOfT, elementTypeArray, false, genericScope)
            };
        }

        private Lazy<Type[]> LazyInterfacesOf(TypeDefinition definition, Lazy<Type> lazyBaseType, GenericScope genericScope) {
            return new Lazy<Type[]>(() => {
                var baseInterfaces = lazyBaseType.Value?.GetInterfaces() ?? System.Type.EmptyTypes;
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

        private IReadOnlyCollection<ILazyMember<MemberInfo>> LazyMembersOf(Type type, TypeDefinition definition, GenericScope genericScope) {
            var members = new List<ILazyMember<MemberInfo>>();
            foreach (var nestedType in definition.NestedTypes) {
                members.Add(new LazyMember<Type>(nestedType.Name, true, nestedType.IsNestedPublic, () => Type(nestedType, genericScope)));
            }

            foreach (var method in definition.Methods) {
                if (method.IsConstructor) {
                    members.Add(new LazyMember<ConstructorInfo>(method.Name, method.IsStatic, method.IsPublic, () => (ConstructorInfo)Method(type, method, method, genericScope)));
                }
                else {
                    members.Add(new LazyMember<MethodInfo>(method.Name, method.IsStatic, method.IsPublic, () => (MethodInfo)Method(type, method, method, genericScope)));
                }
            }

            foreach (var field in definition.Fields) {
                members.Add(new LazyMember<FieldInfo>(field.Name, field.IsStatic, field.IsPublic, () => Field(field, genericScope)));
            }

            if (type.BaseType != null)
                AddMembersFrom(type.BaseType, members);

            if (type.IsInterface) {
                foreach (var @interface in type.GetInterfaces()) {
                    AddMembersFrom(@interface, members);
                }
            }

            return members;
        }

        private void AddMembersFrom(Type type, ICollection<ILazyMember<MemberInfo>> members) {
            var names = members.Select(m => m.Name).ToSet();
            var interpreted = type as InterpretedType;
            if (interpreted != null) {
                foreach (var lazy in interpreted.GetLazyMembers()) {
                    if (names.Contains(lazy.Name))
                        continue;

                    members.Add(lazy);
                }
                return;
            }

            foreach (var member in type.GetMembers()) {
                if (names.Contains(member.Name))
                    continue;

                members.Add(ToLazyMember(member));
            }
        }

        private ILazyMember<MemberInfo> ToLazyMember(MemberInfo member) {
            var type = member as Type;
            if (type != null)
                return new LazyMember<Type>(type.Name, true, type.IsNestedPublic, () => type);

            var constructor = member as ConstructorInfo;
            if (constructor != null)
                return new LazyMember<ConstructorInfo>(constructor.Name, constructor.IsStatic, constructor.IsPublic, () => constructor);

            var method = member as MethodInfo;
            if (method != null)
                return new LazyMember<MethodInfo>(method.Name, method.IsStatic, method.IsPublic, () => method);

            var property = member as PropertyInfo;
            if (property != null) {
                var isStatic = (property.GetGetMethod()?.IsStatic ?? false) || (property.GetSetMethod()?.IsStatic ?? false);
                var isPublic = (property.GetGetMethod()?.IsPublic ?? false) || (property.GetSetMethod()?.IsPublic ?? false);
                return new LazyMember<PropertyInfo>(property.Name, isStatic, isPublic, () => property);
            }

            var field = member as FieldInfo;
            if (field != null)
                return new LazyMember<FieldInfo>(field.Name, field.IsStatic, field.IsPublic, () => field);

            throw new NotImplementedException($"Unsupported memeber type {member.GetType()}.");
        }

        public FieldInfo Field(FieldReference reference, GenericScope genericScope) {
            Argument.NotNull(nameof(reference), reference);

            var definition = reference.Resolve();
            var declaringType = Type(reference.DeclaringType, genericScope);

            genericScope = genericScope.With(definition.DeclaringType.GenericParameters, declaringType.GenericTypeArguments);

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
            var definition = reference.Resolve();
            if (definition == null)
                throw new Exception($"Failed to resolve definition for method {reference}.");

            var declaringType = Type(reference.DeclaringType, genericScope);
            genericScope = genericScope.With(definition.DeclaringType.GenericParameters, declaringType.GenericTypeArguments);
            return Method(declaringType, definition, reference, genericScope);
        }

        private MethodBase Method(Type declaringType, MethodDefinition definition, MethodReference reference, GenericScope genericScope) {
            var genericArguments = Empty<Type>.Array;
            var generic = reference as GenericInstanceMethod;
            if (generic != null) {
                genericArguments = generic.GenericArguments.Select(a => Type(a, genericScope)).ToArray();
                genericScope = genericScope.With(definition.GenericParameters, genericArguments);
            }

            var cacheKey = new MethodKey(declaringType, definition, genericArguments);
            MethodBase method;
            if (_methodCache.TryGetValue(cacheKey, out method))
                return method;

            method = MethodUncached(declaringType, definition, reference, genericArguments, genericScope);
            _methodCache[cacheKey] = method;
            return method;
        }

        private MethodBase MethodUncached(Type declaringType, MethodDefinition definition, MethodReference reference, Type[] genericArguments, GenericScope genericScope) {
            var interpretedType = declaringType as InterpretedType;
            if (ShouldBeRuntime(definition) && interpretedType == null) {
                var token = definition.MetadataToken.ToInt32();
                return (MethodBase)declaringType
                    .GetMembers()
                    .First(m => m.MetadataToken == token);
            }
            if (interpretedType == null)
                throw InterpretedMemberInNonInterpretedType(reference, declaringType);

            var parameters = reference.Parameters.Select(p => Parameter(p, genericScope)).ToArray();
            var attributes = (System.Reflection.MethodAttributes)definition.Attributes;

            if (definition.IsConstructor)
                return new InterpretedConstructor(interpretedType, definition.Name, parameters, attributes, _invoker, definition);

            var overrides = definition.HasOverrides
                ? new Lazy<MethodInfo[]>(() => definition.Overrides.Select(o => (MethodInfo)Method(o, genericScope)).ToArray())
                : LazyEmptyMethods;

            var returnType = Type(reference.ReturnType, genericScope);
            return new InterpretedMethod(
                Module(definition.Module),
                interpretedType,
                reference.Name,
                returnType,
                parameters,
                overrides,
                attributes,
                GenericDetails(definition, genericArguments, genericScope),
                _invoker,
                definition
            );
        }
        
        private ParameterInfo Parameter(ParameterDefinition definition, GenericScope genericScope) 
            => new InterpretedParameter(Type(definition.ParameterType, genericScope));

        private GenericDetails GenericDetails(IGenericParameterProvider definition, Type[] genericArguments, GenericScope genericScope) {
            var genericParameters = definition.GenericParameters.Count > 0
                ? definition.GenericParameters.Select(p => Type(p, genericScope)).ToArray()
                : null;

            return genericParameters != null || genericArguments != null
                ? new GenericDetails(genericArguments != null, genericParameters != null, genericArguments ?? genericParameters)
                : null;
        }
        
        private Exception InterpretedMemberInNonInterpretedType(MemberReference reference, Type type) {
            return new Exception($"{reference} belongs to a non-interpreted type {type} and has to be non-intepreted.");
        }

        private bool HasGenericPath(Type type) {
            return type != null
                && (type.IsConstructedGenericType || HasGenericPath(type.DeclaringType));
        }

        private bool ShouldBeRuntime(AssemblyDefinition assembly) => assembly.Name.Name == "mscorlib";
        private bool ShouldBeRuntime(ModuleDefinition module) => ShouldBeRuntime(module.Assembly);
        private bool ShouldBeRuntime<TDefinition>(TDefinition definition)
            where TDefinition : MemberReference, IMemberDefinition 
            => ShouldBeRuntime(definition.Module);

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

        private struct MethodKey : IEquatable<MethodKey> {
            public Type DeclaringType { get; }
            public MethodDefinition Definition { get; }
            public Type[] GenericArguments { get; }

            public MethodKey(Type declaringType, MethodDefinition definition, Type[] genericArguments) {
                DeclaringType = declaringType;
                Definition = definition;
                GenericArguments = genericArguments;
            }

            public bool Equals(MethodKey other) {
                if (DeclaringType != other.DeclaringType || Definition != other.Definition)
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
                var hashCode = (DeclaringType?.GetHashCode() ?? 0) ^ Definition.GetHashCode();
                for (var i = 0; i < GenericArguments.Length; i++) {
                    hashCode ^= GenericArguments[i].GetHashCode();
                }
                return hashCode;
            }
        }
    }
}