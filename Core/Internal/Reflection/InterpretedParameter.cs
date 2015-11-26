using System;
using System.Reflection;
using Mono.Cecil;

namespace Cilin.Core.Internal.Reflection {
    public class InterpretedParameter : ParameterInfo {
        private readonly Type _parameterType;
        
        public InterpretedParameter(Type parameterType) {
            _parameterType = parameterType;
        }

        public override Type ParameterType {
            get { return _parameterType; }
        }
    }
}