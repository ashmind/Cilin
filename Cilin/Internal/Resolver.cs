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
        private static readonly MemberReferenceEqualityComparer ReferenceComparer = new MemberReferenceEqualityComparer();

        private readonly IDictionary<string, Assembly> _assemblyCache = new Dictionary<string, Assembly>();
        private readonly IDictionary<MemberReference, MemberInfo> _memberInfoCache = new Dictionary<MemberReference, MemberInfo>(ReferenceComparer);
        private readonly IDictionary<ParameterDefinition, ParameterInfo> _parameterCache = new Dictionary<ParameterDefinition, ParameterInfo>();
        private readonly IDictionary<TypeReference, TypeReference> _normalizationCache = new Dictionary<TypeReference, TypeReference>(ReferenceComparer);

        private readonly IDictionary<string, Type> DEBUG_AlreadyAddedTypes = new Dictionary<string, Type>();

        private static readonly TypeReference ObjectDataReference = 
            ModuleDefinition.ReadModule(typeof(CilinObject).Assembly.Location).GetType(typeof(CilinObject).FullName);

        private readonly Interpreter _interpreter;

        public Resolver(Interpreter interpreter) {
            _interpreter = interpreter;
        }

        public Assembly Assembly(AssemblyDefinition assembly) {
            return _assemblyCache.GetOrAdd(assembly.Name.Name, _ => {
                if (!ShouldInterpret(assembly))
                    return typeof(object).Assembly; // TODO

                return new InterpretedAssembly(assembly);
            });
        }

        public Type Type(TypeReference reference, GenericScope genericScope = null) {
            return (Type)_memberInfoCache.GetOrAdd(reference, r => {
                var debugType = TypeUncached((TypeReference)r, genericScope);

                var fullType = (debugType as ErasedWrapperType)?.FullType ?? debugType;
                if (DEBUG_AlreadyAddedTypes.GetOrAdd(fullType.FullName, _ => fullType) != fullType)
                    throw new Exception("Attempted to re-add fulltype " + fullType.FullName + " that already exists as a different instance.");

                return debugType;
            });
        }

        private Type TypeUncached(TypeReference reference, GenericScope genericScope) {
            var normalized = Normalize(reference, genericScope);
            if (!ReferenceComparer.Equals(normalized, reference))
                return Type(normalized, genericScope);

            var definition = reference.Resolve();
            if (!ShouldInterpret(definition, reference)) {
                var erased = EraseInterpretedTypes(reference, definition);
                if (erased != reference) {
                    var fullType = new InterpretedType(reference, definition, _interpreter, this);
                    return new ErasedWrapperType(Type(erased), fullType);
                }

                var fullName = TypeSupport.GetFullName(erased);
                try {
                    return System.Type.GetType(fullName + ", " + definition.Module.Assembly.Name, true);
                }
                catch (Exception ex) {
                    throw new Exception($"Failed to resolve type '{fullName}': {ex.Message}.", ex);
                }
            }

            return new InterpretedType(reference, definition, _interpreter, this);
        }

        private TypeReference Normalize(TypeReference reference, GenericScope genericScope) {
            return _normalizationCache.GetOrAdd(reference, r => NormalizeUncached(reference, genericScope));
        }

        private TypeReference NormalizeUncached(TypeReference reference, GenericScope genericScope) {
            if (reference.IsArray) {
                var elementType = reference.GetElementType();
                var newElementType = Normalize(reference.GetElementType(), genericScope);
                if (ReferenceComparer.Equals(newElementType, elementType))
                    return reference;

                return new ArrayType(newElementType);
            }

            var parameter = reference as GenericParameter;
            if (parameter != null) {
                if (genericScope == null)
                    throw new Exception($"Generic scope is required to resolve type {parameter}.");

                return genericScope.GetArgumentType(parameter);
            }

            var generic = reference as GenericInstanceType;
            if (generic != null) {
                var newGeneric = new GenericInstanceType(generic.GetElementType());
                foreach (var argument in generic.GenericArguments) {
                    var newArgument = Normalize(argument, genericScope);
                    newGeneric.GenericArguments.Add(newArgument);
                }
                if (ReferenceComparer.Equals(newGeneric, generic))
                    return generic;

                return newGeneric;
            }

            return reference;
        }

        private TypeReference EraseInterpretedTypes(TypeReference reference, TypeDefinition definition) {
            if (reference.IsArray) {
                var oldElementType = reference.GetElementType();
                var newElementType = EraseInterpretedTypes(oldElementType, oldElementType.Resolve());
                return (newElementType != oldElementType) ? new ArrayType(newElementType) : reference;
            }

            var generic = reference as GenericInstanceType;
            if (generic != null) {
                var newGeneric = new GenericInstanceType(generic.GetElementType());
                var hasErased = false;
                var oldArguments = generic.GenericArguments;
                for (var i = 0; i < oldArguments.Count; i++) {
                    var oldArgument = oldArguments[i];
                    var newArgument = EraseInterpretedTypes(oldArgument, oldArgument.Resolve());
                    if (newArgument != oldArgument)
                        hasErased = true;

                    newGeneric.GenericArguments.Add(newArgument);
                }

                return hasErased ? newGeneric : reference;
            }

            if (ShouldInterpret(definition, reference))
                return ObjectDataReference;

            return reference;
        }

        public FieldInfo Field(FieldReference reference, GenericScope genericScope = null) {
            return (FieldInfo)_memberInfoCache.GetOrAdd(reference, r => FieldUncached((FieldReference)r, genericScope));
        }

        private FieldInfo FieldUncached(FieldReference reference, GenericScope genericScope = null) {
            var definition = reference.Resolve();
            var type = Type(reference.DeclaringType, genericScope);

            var interpretedType = type as InterpretedType;
            if (!ShouldInterpret(definition, reference) && interpretedType == null) {
                var token = reference.MetadataToken.ToInt32();
                return (FieldInfo)type
                    .GetMembers()
                    .First(m => m.MetadataToken == token);
            }
            if (interpretedType == null)
                throw InterpretedMemberInNonInterpretedType(reference, type);

            return new InterpretedField(reference, definition, interpretedType, this);
        }

        public MethodBase Method(MethodReference reference, GenericScope genericScope = null, Type declaringType = null) {
            return Method(reference, null, genericScope, declaringType);
        }

        public MethodBase Method(MethodReference reference, MethodDefinition definition, GenericScope genericScope = null, Type declaringType = null) {
            MemberInfo result;
            if (_memberInfoCache.TryGetValue(reference, out result))
                return (MethodBase)result;

            var method = MethodUncached(reference, definition, genericScope, declaringType);
            _memberInfoCache.Add(reference, method);
            return method;

            //return (MethodBase)_memberInfoCache.GetOrAdd(reference, r => MethodUncached((MethodReference)r, definition, genericScope));
        }

        private MethodBase MethodUncached(MethodReference reference, MethodDefinition definition = null, GenericScope genericScope = null, Type declaringType = null) {
            definition = definition ?? reference.Resolve();
            if (definition == null)
                throw new Exception($"Failed to resolve defiition for method {reference}.");

            //var normalized = Normalize(reference, definition, genericScope);
            //if (!ReferenceComparer.Equals(normalized, reference))
                //return Method(normalized, definition, genericScope);

            declaringType = declaringType ?? Type(reference.DeclaringType, genericScope);
            var interpretedType = declaringType as InterpretedType;

            if (!ShouldInterpret(definition, reference) && interpretedType == null) {
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

            return definition.IsConstructor
                 ? (MethodBase)new InterpretedConstructor(definition, interpretedType, _interpreter, this)
                 : new InterpretedMethod(reference, definition, interpretedType, _interpreter, this);
        }

        private MethodReference Normalize(MethodReference reference, MethodDefinition definition, GenericScope genericScope) {
            var generic = reference as GenericInstanceMethod;
            var newDeclaringType = Normalize(reference.DeclaringType, genericScope);
            var newDeclaringTypeArguments = (newDeclaringType as GenericInstanceType)?.GenericArguments;
            if (newDeclaringTypeArguments != null)
                genericScope = new GenericScope(definition.DeclaringType, newDeclaringTypeArguments.ToArray(), genericScope);

            var genericArguments = generic?.GenericArguments;
            var newGenericArguments = generic?.GenericArguments.Select(a => Normalize(a, genericScope)).ToArray();
            if (newGenericArguments != null)
                genericScope = new GenericScope(definition, newGenericArguments, genericScope);

            var newParameters = new List<ParameterDefinition>();
            var hasNewParameters = false;
            foreach (var parameter in reference.Parameters) {
                var newParameterType = Normalize(parameter.ParameterType, genericScope);
                var newParameter = (ReferenceComparer.Equals(newParameterType, parameter.ParameterType))
                                 ? parameter
                                 : new ParameterDefinition(parameter.Name, parameter.Attributes, newParameterType);
                newParameters.Add(newParameter);
                hasNewParameters = hasNewParameters | newParameter != parameter;
            }

            if (ReferenceComparer.Equals(newDeclaringType, reference.DeclaringType) && (genericArguments?.All(a => !a.IsGenericParameter) ?? true) && !hasNewParameters)
                return reference;

            var newReference = new MethodReference(reference.Name, Normalize(reference.ReturnType, genericScope)) {
                CallingConvention = reference.CallingConvention,
                ExplicitThis = reference.ExplicitThis,
                DeclaringType = newDeclaringType,
                HasThis = reference.HasThis,
                MetadataToken = reference.MetadataToken,
                MethodReturnType = reference.MethodReturnType
            };
            newReference.GenericParameters.AddRange(reference.GenericParameters);
            newReference.Parameters.AddRange(newParameters);

            if (generic != null) {
                var newGeneric = new GenericInstanceMethod(newReference);
                newGeneric.GenericArguments.AddRange(newGenericArguments);
                newGeneric.MetadataToken = reference.MetadataToken;

                newReference = newGeneric;
            }

            return newReference;
        }

        private MethodBase SpecialMethodUncached(Type declaringType, MethodReference reference) {

            return null;
        }

        public ParameterInfo Parameter(ParameterDefinition definition, GenericScope genericScope = null) {
            return _parameterCache.GetOrAdd(definition, d => {
                return new InterpretedParameter(d, this, genericScope);
            });
        }
        
        private bool ShouldInterpret(TypeDefinition definition, TypeReference reference) {
            return ShouldInterpret<TypeDefinition, TypeReference>(definition, reference)
                && !reference.IsArray;
        }

        private bool ShouldInterpret<TDefinition, TReference>(TDefinition definition, TReference reference)
            where TDefinition: TReference, IMemberDefinition
            where TReference : MemberReference
        {
            return ShouldInterpret(definition.Module.Assembly);
        }

        private bool ShouldInterpret(AssemblyDefinition assembly) {
            return assembly.Name.Name != "mscorlib";
        }

        private Exception InterpretedMemberInNonInterpretedType(MemberReference reference, Type type) {
            return new Exception($"{reference} belongs to a non-interpreted type {type} and has to be non-intepreted.");
        }
    }
}