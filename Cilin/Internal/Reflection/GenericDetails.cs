using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.Reflection {
    public class GenericDetails {
        public GenericDetails(bool isConstructed, bool hasGenericParameters, Type[] parametersOrArguments) {
            IsConstructed = isConstructed;
            HasGenericParameters = hasGenericParameters;
            ParametersOrArguments = parametersOrArguments;
        }

        public bool IsConstructed { get; }
        public bool HasGenericParameters { get; }
        public Type[] ParametersOrArguments { get; }
    }
}
