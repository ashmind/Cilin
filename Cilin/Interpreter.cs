using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AshMind.Extensions;
using Cilin.Internal;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilin {
    public class Interpreter {
        private readonly IReadOnlyDictionary<OpCode, ICilHandler> _handlers;
        private readonly MethodInvoker _invoker;
        private readonly Resolver _resolver;

        public Interpreter() : this(
            typeof(Interpreter).Assembly
                .GetTypes()
                .Where(t => t.HasInterface<ICilHandler>() && !t.IsInterface)
                .Select(t => (ICilHandler)Activator.CreateInstance(t))
        ) {}

        public Interpreter(IEnumerable<ICilHandler> handlers) {
            var handlersByCode = new Dictionary<OpCode, ICilHandler>();
            foreach (var handler in handlers) {
                foreach (var opCode in handler.GetOpCodes()) {
                    handlersByCode.Add(opCode, handler);
                }
            }
            _handlers = handlersByCode;
            _invoker = new MethodInvoker(this);
            _resolver = new Resolver(_invoker);
        }

        public object InterpretCall(IReadOnlyList<Type> declaringTypeArguments, MethodDefinition method, IReadOnlyList<Type> typeArguments, object target, IReadOnlyList<object> arguments) {
            ValidateCall(method, declaringTypeArguments, typeArguments);
            var genericScope = GenericScope.None
                .With(method.DeclaringType.GenericParameters, declaringTypeArguments)
                .With(method.GenericParameters, typeArguments);

            return InterpretCall(genericScope, method, target, arguments);
        }

        public object InterpretCall(IReadOnlyList<TypeReference> declaringTypeArguments, MethodDefinition method, IReadOnlyList<TypeReference> typeArguments, object target, IReadOnlyList<object> arguments) {
            ValidateCall(method, declaringTypeArguments, typeArguments);
            var genericTypeScope = GenericScope.None
                .With(method.DeclaringType.GenericParameters, declaringTypeArguments.Select(a => _resolver.Type(a, GenericScope.None)));            
            var genericScope = genericTypeScope.With(method.GenericParameters, typeArguments.Select(a => _resolver.Type(a, genericTypeScope)));

            return InterpretCall(genericScope, method, target, arguments);
        }

        private object InterpretCall(GenericScope genericScope, MethodDefinition method, object target, IReadOnlyList<object> arguments) {
            var context = new CilHandlerContext(genericScope, method, target, arguments ?? Empty<object>.Array, _resolver, _invoker);
            var instruction = method.Body.Instructions[0];
            var returnType = _resolver.Type(method.ReturnType, genericScope);

            while (instruction != null) {
                if (instruction.OpCode == OpCodes.Ret) {
                    var result = (object)null;
                    if (returnType != typeof(void))
                        result = TypeSupport.Convert(context.Stack.Pop(), returnType);

                    if (context.Stack.Count > 0)
                        throw new Exception($"Unbalanced stack on return: {context.Stack.Count} extra items.");

                    return result;
                }

                context.NextInstruction = instruction.Next;
                InterpretInstruction(instruction, context);
                instruction = context.NextInstruction;
            }

            throw new Exception($"Failed to reach a 'ret' instruction.");
        }

        private void ValidateCall(MethodDefinition method, IReadOnlyList<object> declaringTypeArguments, IReadOnlyList<object> typeArguments) {
            if (method.IsInternalCall)
                throw new ArgumentException($"Cannot interpret InternalCall method {method}.", nameof(method));
            if (!method.HasBody)
                throw new ArgumentException($"Cannot interpret method {method} that has no Body.", nameof(method));
            if (declaringTypeArguments.Count != method.DeclaringType.GenericParameters.Count)
                throw new ArgumentException($"Type {method.DeclaringType} requires {method.DeclaringType.GenericParameters.Count} type arguments, but got {declaringTypeArguments.Count}.");
            if (typeArguments.Count != method.GenericParameters.Count)
                throw new ArgumentException($"Method {method} requires {method.GenericParameters.Count} type arguments, but got {typeArguments.Count}.");
        }

        private void InterpretInstruction(Instruction instruction, CilHandlerContext context) {
            var handler = _handlers.GetValueOrDefault(instruction.OpCode);
            if (handler == null)
                throw new NotImplementedException($"Instruction {instruction.OpCode} is not implemented.");

            handler.Handle(instruction, context);
        }
    }
}
