using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AshMind.Extensions;
using Mono.Cecil;

namespace Cilin.Internal {
    public class GenericScope {
        private readonly IReadOnlyDictionary<GenericParameter, Type> _map;
        private readonly GenericScope _parent;

        public GenericScope(IEnumerable<GenericParameter> parameters, IEnumerable<Type> arguments, GenericScope parent = null) {
            _parent = parent;

            var map = new Dictionary<GenericParameter, Type>();
            using (var parametersEnumerator = parameters.GetEnumerator())
            using (var argumentsEnumerator = arguments.GetEnumerator()) {
                var count = 0;
                while (parametersEnumerator.MoveNext()) {
                    count += 1;
                    if (!argumentsEnumerator.MoveNext())
                        throw new ArgumentException($"Expected more than {count} arguments, got {count}.", nameof(arguments));

                    map[parametersEnumerator.Current] = argumentsEnumerator.Current;
                }

                if (argumentsEnumerator.MoveNext())
                    throw new ArgumentException($"Too many arguments: expected {count}.", nameof(arguments));
            }

            _map = map;
        }

        public GenericScope(IReadOnlyDictionary<GenericParameter, Type> map, GenericScope parent = null) {
            _map = map;
            _parent = parent;
        }

        public Type Resolve(GenericParameter parameter) {
            return _map.GetValueOrDefault(parameter)
                ?? _parent?.Resolve(parameter);
        }
    }
}
