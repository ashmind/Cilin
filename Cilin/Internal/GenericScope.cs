using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AshMind.Extensions;
using Mono.Cecil;

namespace Cilin.Internal {
    public class GenericScope {
        private readonly IReadOnlyCollection<GenericMap> _map;
        private readonly GenericScope _parent;

        public GenericScope(IEnumerable<GenericParameter> parameters, IEnumerable<Type> arguments, GenericScope parent = null) {
            _parent = parent;

            var map = new List<GenericMap>();
            using (var parametersEnumerator = parameters.GetEnumerator())
            using (var argumentsEnumerator = arguments.GetEnumerator()) {
                var count = 0;
                while (parametersEnumerator.MoveNext()) {
                    count += 1;
                    if (!argumentsEnumerator.MoveNext())
                        throw new ArgumentException($"Expected more than {count} arguments, got {count}.", nameof(arguments));

                    var parameter = parametersEnumerator.Current;
                    map.Add(new GenericMap(parameter.Owner, parameter.Position, argumentsEnumerator.Current));
                }

                if (argumentsEnumerator.MoveNext())
                    throw new ArgumentException($"Too many arguments: expected {count}.", nameof(arguments));
            }

            _map = map;
        }
        
        public Type Resolve(GenericParameter parameter) {
            foreach (var entry in _map) {
                var entryOwner = entry.ParameterOwner;
                if ((entryOwner == parameter.Owner || entryOwner == Resolve(parameter.Owner)) && entry.ParameterPosition == parameter.Position)
                    return entry.ArgumentType;
            }
            return _parent?.Resolve(parameter);
        }

        private IGenericParameterProvider Resolve(IGenericParameterProvider owner) {
            if (owner.IsDefinition)
                return owner;

            return (IGenericParameterProvider)(owner as TypeReference)?.Resolve()
                ?? ((MethodReference)owner).Resolve();
        }

        private class GenericMap {
            public GenericMap(IGenericParameterProvider parameterOwner, int parameterPosition, Type argumentType) {
                ParameterOwner = parameterOwner;
                ParameterPosition = parameterPosition;
                ArgumentType = argumentType;
            }

            public IGenericParameterProvider ParameterOwner { get; }
            public int ParameterPosition { get; }
            public Type ArgumentType { get; }
        }
    }
}
