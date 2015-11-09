using System;
using System.Reflection;
using Mono.Cecil;

namespace Cilin.Internal.Reflection {
    public class InterpretedParameter : ParameterInfo {
        private readonly ParameterDefinition _definition;
        private readonly Type _parameterType;
        private readonly GenericScope _genericScope;
        
        public InterpretedParameter(ParameterDefinition definition, Resolver resolver, GenericScope genericScope) {
            _definition = definition;
            _parameterType = resolver.Type(definition.ParameterType, genericScope);
            _genericScope = genericScope;
        }

        public override Type ParameterType {
            get { return _parameterType; }
        }
    }
}