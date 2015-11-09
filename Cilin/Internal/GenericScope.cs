using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Cilin.Internal {
    public class GenericScope {
        private readonly IMemberDefinition _definition;
        private readonly IReadOnlyList<TypeReference> _arguments;
        private readonly GenericScope _parent;

        public GenericScope(IMemberDefinition definition, IReadOnlyList<TypeReference> arguments, GenericScope parent = null) {
            var unresolved = arguments.OfType<GenericParameter>().FirstOrDefault();
            if (unresolved != null)
                throw new ArgumentException($"Cannot include unresolved parameter {unresolved} into a GenericScope of {definition}.");

            _definition = definition;
            _arguments = arguments;
            _parent = parent;
        }

        public TypeReference GetArgumentType(GenericParameter parameter) {
            var ownerDefinition = (IMemberDefinition)(parameter.Owner as MethodReference)?.Resolve()
                               ?? (parameter.Owner as TypeReference)?.Resolve();
            if (ownerDefinition != _definition) {
                if (_parent != null)
                    return _parent.GetArgumentType(parameter);

                throw new NotSupportedException($"Could not find generic argument for parameter {parameter} owned by {parameter.Owner}.");
            }

            return _arguments[parameter.Position];
        }
    }
}
