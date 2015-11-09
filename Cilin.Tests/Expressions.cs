using System;
using System.Linq;
using Cilin.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cilin.Tests {
    public class Expressions {
        [InterpreterTheory]
        [InlineData]
        public int LiteralInt32() {
            return 5;
        }

        [InterpreterTheory]
        [InlineData]
        public int LiteralInt32Unchecked() {
            return unchecked((int)uint.MaxValue);
        }

        [InterpreterTheory]
        [InlineData]
        public string LiteralString() {
            return "x";
        }

        [InterpreterTheory]
        [InlineData]
        public bool LiteralBoolean() {
            return true;
        }

        [InterpreterTheory]
        [InlineData(5, 5)]
        public int OperatorPlus(int x, int y) {
            return x + y;
        }

        [InterpreterTheory]
        [InlineData(int.MaxValue, 5)]
        public int OperatorPlusUnchecked(int x, int y) {
            return unchecked(x + y);
        }
    }
}
