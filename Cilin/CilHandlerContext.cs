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

        public CilHandlerContext(IReadOnlyList<TypeReference> declaringTypeArguments, MethodDefinition method, IReadOnlyList<TypeReference> typeArguments, object target, IReadOnlyList<object> arguments, Resolver resolver) {
            _variableDefinitions = method.Body.Variables.OrderBy(v => v.Index).ToList();
            var variables = new List<object>(new object[_variableDefinitions.Count]);
            Variables = variables;
            _variablesMutable = variables;

            Resolver = resolver;

            DeclaringTypeArguments = declaringTypeArguments;
            Method = method;
            TypeArguments = typeArguments;
            Target = target;
            Arguments = arguments;

            GenericScope = Resolver.GenericScope(
                method.GenericParameters.AsReadOnlyList(),
                typeArguments.AsReadOnlyList(),
                Resolver.GenericScope(
                    method.DeclaringType.GenericParameters.AsReadOnlyList(),
                    declaringTypeArguments.AsReadOnlyList(),
                    null
                )
            );
        }

        public Resolver Resolver { get; }
        public IReadOnlyList<TypeReference> DeclaringTypeArguments { get; }
        public MethodDefinition Method { get; }
        public IReadOnlyList<TypeReference> TypeArguments { get; }
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
