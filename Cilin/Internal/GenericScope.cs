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
