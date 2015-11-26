﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Cilin.Core.Internal.Reflection;

namespace Cilin.Core.Internal.Handlers {
    public class NewArrHandler : ICilHandler {
        public IEnumerable<OpCode> GetOpCodes() {
            yield return OpCodes.Newarr;
        }

        public void Handle(Instruction instruction, CilHandlerContext context) {
            var length = (int)context.Stack.Pop();
            var arrayType = context.Resolver.Type(new ArrayType((TypeReference)instruction.Operand), context.GenericScope);

            context.Stack.Push(TypeSupport.CreateArray(arrayType, length));
        }
    }
}