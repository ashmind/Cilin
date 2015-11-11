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
        private static readonly Lazy<Type> LazyTypeNull = new Lazy<Type>(() => null);
        private static readonly Lazy<Type> LazyTypeArray = new Lazy<Type>(() => typeof(Array));

        private static readonly MemberReferenceEqualityComparer ReferenceComparer = new MemberReferenceEqualityComparer();

        private readonly IDictionary<string, Assembly> _assemblyCache = new Dictionary<string, Assembly>();
        private readonly IDictionary<MemberReference, MemberInfo> _memberInfoCache = new Dictionary<MemberReference, MemberInfo>(ReferenceComparer);

        private static readonly TypeReference ObjectDataReference = 
            ModuleDefinition.ReadModule(typeof(CilinObject).Assembly.Location).GetType(typeof(CilinObject).FullName);

        private readonly Interpreter _interpreter;

        public Resolver(Interpreter interpreter) {
            _interpreter = interpreter;
        }

        public Assembly Assembly(AssemblyDefinition assembly) {
            Argument.NotNull(nameof(assembly), assembly);
            return _assemblyCache.GetOrAdd(assembly.Name.Name, _ => {
                if (ShouldBeRuntime(assembly))
                    return typeof(object).Assembly; // TODO

                return new InterpretedAssembly(assembly);
            });
        }

        public Type Type(TypeReference reference, GenericScope genericScope) {
            Argument.NotNull(nameof(reference), reference);

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

            var generic = reference as GenericInstanceType;
            if (generic != null) {
                var genericDefinition = Type(definition, genericScope);

                var arguments = new Type[generic.GenericArguments.Count];
                bool allArgumentsAreRuntime = false;
                for (var i = 0; i < generic.GenericArguments.Count; i++) {
                    var resolved = Type(generic.GenericArguments[i], genericScope);
                    allArgumentsAreRuntime = allArgumentsAreRuntime && IsRuntime(resolved);
                    arguments[i] = resolved;
                }

                if (IsRuntime(genericDefinition)) {
                    if (allArgumentsAreRuntime)
                        return genericDefinition.MakeGenericType(arguments);

                    var erased = new Type[arguments.Length];
                    for (var i = 0; i < arguments.Length; i++) {
                        erased[i] = IsRuntime(arguments[i]) ? arguments[i] : typeof(CilinObject);
                    }
                    var erasedFull = genericDefinition.MakeGenericType(erased);
                    return new ErasedWrapperType(erasedFull, NewInterpretedType(definition, genericScope, arguments));
                }

                return NewInterpretedType(definition, genericScope, arguments);
            }

            if (ShouldBeRuntime(definition, reference)) {
                var fullName = TypeSupport.GetFullName(reference);
                try {
                    return System.Type.GetType(fullName + ", " + definition.Module.Assembly.Name, true);
                }
                catch (Exception ex) {
                    throw new Exception($"Failed to resolve type '{fullName}': {ex.Message}.", ex);
                }
            }

            return NewInterpretedType(definition, genericScope);
        }

        private IReadOnlyCollection<LazyMember> LazyMembersOf(TypeDefinition definition, GenericScope genericScope) {
            var list = new List<LazyMember>();
            foreach (var method in definition.Methods) {
                if (method.IsConstructor) {
                    list.Add(new LazyMember<ConstructorInfo>(method.Name, method.IsStatic, () => (ConstructorInfo)Method(method, genericScope)));
                }
                else {
                    list.Add(new LazyMember<MethodInfo>(method.Name, method.IsStatic, () => (MethodInfo)Method(method, genericScope)));
                }
            }

            foreach (var field in definition.Fields) {
                list.Add(new LazyMember<FieldInfo>(field.Name, field.IsStatic, () => Field(field, genericScope)));
            }

            return list;
        }

        private InterpretedType NewInterpretedType(TypeDefinition definition, GenericScope genericScope, Type[] genericArguments = null) {
            return new InterpretedType(
                definition.Name,
                definition.Namespace,
                Assembly(definition.Module.Assembly),
                definition.DeclaringType != null ? new Lazy<Type>(() => Type(definition.DeclaringType, genericScope)) : LazyTypeNull,
                new Lazy<Type>(() => Type(definition.BaseType, genericScope)),
                new Lazy<Type[]>(() => definition.Interfaces.Select(i => Type(i, genericScope)).ToArray()),
                null,
                LazyMembersOf(definition, genericScope),
                (System.Reflection.TypeAttributes)definition.Attributes,
                genericArguments != null ? new GenericDetails(genericArguments) : null
            );
        }
        
        private InterpretedType NewIntepretedArrayType(TypeReference elementTypeReference, Type elementType, GenericScope genericScope) {
            return new InterpretedType(
                elementType.Name + "[]",
                elementType.Namespace,
                elementType.Assembly,
                LazyTypeNull,
                LazyTypeArray,
                new Lazy<Type[]>(() => TypeSupport.GetArrayInterfaces(elementTypeReference).Select(i => Type(i, genericScope)).ToArray()),
                null,
                new LazyMember[0],
                typeof(Array).Attributes,
                null
            );
        }

        public FieldInfo Field(FieldReference reference, GenericScope genericScope = null) {
            Argument.NotNull(nameof(reference), reference);
            return (FieldInfo)_memberInfoCache.GetOrAdd(reference, r => FieldUncached((FieldReference)r, genericScope));
        }

        private FieldInfo FieldUncached(FieldReference reference, GenericScope genericScope = null) {
            var definition = reference.Resolve();
            var type = Type(reference.DeclaringType, genericScope);

            var interpretedType = type as InterpretedType;
            if (ShouldBeRuntime(definition, reference) && interpretedType == null) {
                var token = reference.MetadataToken.ToInt32();
                return (FieldInfo)type
                    .GetMembers()
                    .First(m => m.MetadataToken == token);
            }
            if (interpretedType == null)
                throw InterpretedMemberInNonInterpretedType(reference, type);

            return new InterpretedField(reference, definition, interpretedType, this);
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
            if (reference.DeclaringType.IsGenericInstance) {
                genericScope = new GenericScope(
                    definition.DeclaringType.GenericParameters,
                    declaringType.GetGenericArguments(),
                    genericScope
                );
            }

            var interpretedType = declaringType as InterpretedType;

            if (ShouldBeRuntime(definition, reference) && interpretedType == null) {
                var special = SpecialMethodUncached(declaringType, reference);
                if (special != null)
                    return special;

                var token = definition.MetadataToken.ToInt32();
                return (MethodBase)declaringType
                    .GetMembers()
                    .First(m => m.MetadataToken == token);
            }
            if (interpretedType == null)
                throw InterpretedMemberInNonInterpretedType(reference, declaringType);

            var generic = reference as GenericInstanceMethod;
            if (generic != null) {
                genericScope = new GenericScope(
                    definition.GenericParameters,
                    generic.GenericArguments.Select(a => Type(a, genericScope)),
                    genericScope
                );
            }

            var parameters = reference.Parameters.Select(p => Parameter(p, genericScope)).ToArray();
            var attributes = (System.Reflection.MethodAttributes)definition.Attributes;
            Func<object, object[], object> invoke = (target, arguments) => {
                var typeArguments = (reference as GenericInstanceMethod)?.GenericArguments.ToArray()
                                 ?? Empty<TypeReference>.Array;

                return _interpreter.InterpretCall(interpretedType.GetGenericArguments(), definition, typeArguments, target, arguments);
            };
            if (definition.IsConstructor)
                return new InterpretedConstructor(interpretedType, parameters, attributes, invoke);

            var returnType = Type(reference.ReturnType, genericScope);
            return new InterpretedMethod(interpretedType, reference.Name, returnType, parameters, attributes, invoke);
        }

        private MethodBase SpecialMethodUncached(Type declaringType, MethodReference reference) {
            return null;
        }

        private ParameterInfo Parameter(ParameterDefinition definition, GenericScope genericScope) {
            return new InterpretedParameter(Type(definition.ParameterType, genericScope));
        }

        private Exception InterpretedMemberInNonInterpretedType(MemberReference reference, Type type) {
            return new Exception($"{reference} belongs to a non-interpreted type {type} and has to be non-intepreted.");
        }

        private bool ShouldBeRuntime(AssemblyDefinition assembly) {
            return assembly.Name.Name == "mscorlib";
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
    }
}