using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AshMind.Extensions;
using Cilin.Internal;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Cilin {
    public class CilHandlerContext {
        private readonly IReadOnlyList<VariableDefinition> _variableDefinitions;
        private readonly IList<object> _variablesMutable;

        public CilHandlerContext(GenericScope genericScope, MethodDefinition method, object target, IReadOnlyList<object> arguments, Resolver resolver) {
            _variableDefinitions = method.Body.Variables.OrderBy(v => v.Index).ToList();
            var variables = new List<object>(new object[_variableDefinitions.Count]);
            Variables = variables;
            _variablesMutable = variables;

            Resolver = resolver;
            Method = method;
            Target = target;
            Arguments = arguments;
            GenericScope = genericScope;
        }

        public Resolver Resolver { get; }
        public MethodDefinition Method { get; }
        public object Target { get; }
        public IReadOnlyList<object> Arguments { get; }
        public IReadOnlyList<object> Variables { get; }
        public Stack<object> Stack { get; } = new Stack<object>();
        public GenericScope GenericScope { get; }
        public Instruction NextInstruction { get; set; }

        public void SetVariable(int index, object value) {
            var definition = _variableDefinitions[index];
            _variablesMutable[index] = TypeSupport.Convert(value, Resolver.Type(definition.VariableType, GenericScope));
        }
    }
}
