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
        private readonly IDictionary<TypeReference, TypeReference> _normalizationCache = new Dictionary<TypeReference, TypeReference>(ReferenceComparer);

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

        public Type Type(TypeReference reference, GenericScope genericScope) {
            return (Type)_memberInfoCache.GetOrAdd(reference, r => {
                return TypeUncached((TypeReference)r, genericScope);
            });
        }

        private Type TypeUncached(TypeReference reference, GenericScope genericScope) {
            //var normalized = Normalize(reference, genericScope);
            //if (!ReferenceComparer.Equals(normalized, reference))
            //    return Type(normalized, genericScope);

            var parameter = reference as GenericParameter;
            if (reference is GenericParameter)
                return genericScope.Resolve(parameter) ?? new Reflection.GenericParameterType(reference.Name);

            var definition = reference.Resolve();
            if (reference.IsArray) {
                var interpreted = new InterpretedType(reference, definition, _interpreter, this);
                if (!ShouldInterpret(definition, reference))
                    return new ErasedWrapperType(typeof(CilinObject).MakeArrayType(), interpreted);

                return interpreted;
            }
            
            if (!ShouldInterpret(definition, reference)) {
                var erased = EraseInterpretedTypes(reference, definition);
                if (erased != reference) {
                    var fullType = new InterpretedType(reference, definition, _interpreter, this);
                    return new ErasedWrapperType(Type(erased, genericScope), fullType);
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

        //private TypeReference Normalize(TypeReference reference, GenericScope genericScope) {
        //    return _normalizationCache.GetOrAdd(reference, r => NormalizeUncached(reference, genericScope));
        //}

        //private TypeReference NormalizeUncached(TypeReference reference, GenericScope genericScope) {
        //    if (reference.IsArray) {
        //        var elementType = reference.GetElementType();
        //        var newElementType = Normalize(reference.GetElementType(), genericScope);
        //        if (ReferenceComparer.Equals(newElementType, elementType))
        //            return reference;

        //        return new ArrayType(newElementType);
        //    }

        //    var parameter = reference as GenericParameter;
        //    if (parameter != null) {
        //        if (genericScope == null)
        //            throw new Exception($"Generic scope is required to resolve type {parameter}.");

        //        return genericScope.Resolve(parameter);
        //    }

        //    var generic = reference as GenericInstanceType;
        //    if (generic != null) {
        //        var newGeneric = new GenericInstanceType(generic.GetElementType());
        //        foreach (var argument in generic.GenericArguments) {
        //            var newArgument = Normalize(argument, genericScope);
        //            newGeneric.GenericArguments.Add(newArgument);
        //        }
        //        if (ReferenceComparer.Equals(newGeneric, generic))
        //            return generic;

        //        return newGeneric;
        //    }

        //    return reference;
        //}

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
        
        public MethodBase Method(MethodReference reference, GenericScope genericScope) {
            MemberInfo result;
            if (_memberInfoCache.TryGetValue(reference, out result))
                return (MethodBase)result;

            var method = MethodUncached(reference, genericScope);
            _memberInfoCache.Add(reference, method);
            return method;
        }

        private MethodBase MethodUncached(MethodReference reference, GenericScope genericScope) {
            var definition = reference.Resolve();
            if (definition == null)
                throw new Exception($"Failed to resolve definition for method {reference}.");

            var declaringType = Type(reference.DeclaringType, genericScope);
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

            var generic = reference as GenericInstanceMethod;
            if (generic != null) {
                genericScope = GenericScope(
                    definition.GenericParameters.AsReadOnlyList(),
                    generic.GenericArguments.AsReadOnlyList(),
                    genericScope
                );
            }

            var parameters = reference.Parameters.Select(p => Parameter(p, genericScope)).ToArray();
            var attributes = (System.Reflection.MethodAttributes)definition.Attributes;
            Func<object, object[], object> invoke = (target, arguments) => {
                var declaringTypeArguments = (interpretedType.ToTypeReference() as GenericInstanceType)?.GenericArguments.ToArray()
                                           ?? Empty<TypeReference>.Array;
                var typeArguments = (reference as GenericInstanceMethod)?.GenericArguments.ToArray()
                                 ?? Empty<TypeReference>.Array;

                return _interpreter.InterpretCall(declaringTypeArguments, definition, typeArguments, target, arguments);
            };
            if (definition.IsConstructor)
                return new InterpretedConstructor(definition, interpretedType, _interpreter, this);

            var returnType = Type(reference.ReturnType, genericScope);
            return new InterpretedMethod(interpretedType, reference.Name, returnType, parameters, attributes, invoke);
        }

        private MethodBase SpecialMethodUncached(Type declaringType, MethodReference reference) {
            return null;
        }

        private ParameterInfo Parameter(ParameterDefinition definition, GenericScope genericScope) {
            return new InterpretedParameter(Type(definition.ParameterType, genericScope));
        }
        
        public GenericScope GenericScope(IReadOnlyList<GenericParameter> parameters, IReadOnlyList<TypeReference> arguments, GenericScope parent) {
            var fullMap = new Dictionary<GenericParameter, Type>();
            for (var i = 0; i < parameters.Count; i++) {
                fullMap[parameters[i]] = Type(arguments[i], parent);
            }
            return new GenericScope(fullMap, parent);
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