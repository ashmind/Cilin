﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Cilin.Core.Internal;
using System.Reflection;

namespace Cilin.Core {
    public class CilHandlerContext {
        private readonly IReadOnlyList<VariableDefinition> _variableDefinitions;
        private readonly IList<object> _variablesMutable;

        public CilHandlerContext(GenericScope genericScope, MethodBase method, MethodDefinition definition, object target, IReadOnlyList<object> arguments, Resolver resolver, MethodInvoker invoker) {
            _variableDefinitions = definition.Body.Variables.OrderBy(v => v.Index).ToList();
            var variables = new List<object>(new object[_variableDefinitions.Count]);
            Variables = variables;
            _variablesMutable = variables;

            Resolver = resolver;
            Invoker = invoker;
            Method = method;
            Definition = definition;
            Target = target;
            Arguments = arguments;
            GenericScope = genericScope;
        }

        public Resolver Resolver { get; }
        public MethodInvoker Invoker { get; }
        public MethodBase Method { get; }
        public MethodDefinition Definition { get; }
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
