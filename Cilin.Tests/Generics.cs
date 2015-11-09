using System;
using System.Collections.Generic;
using System.Linq;
using Cilin.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cilin.Tests {
    public class Generics {
        private class LocalClass { }
        public class Generic<T> {
        }

        public static class NonGeneric {
            public static void GenericVoid<T>() { }
            public static T GenericReturn<T>(T value) { return value; }
            public static Generic<T> GenericReturnNestedLocal<T>() { return null; }
        }

        [InterpreterTheory]
        [InlineData]
        public void Generic_Void_LocalType() {
            NonGeneric.GenericVoid<LocalClass>();
        }

        [InterpreterTheory]
        [InlineData]
        public void Generic_Void_SystemType() {
            NonGeneric.GenericVoid<int>();
        }

        [InterpreterTheory]
        [InlineData]
        public int Generic_Result_SystemType() {
            return NonGeneric.GenericReturn(5);
        }

        [InterpreterTheory]
        [InlineData(5)]
        [InlineData("x")]
        public T Generic_Result_InGenericTestMethod<T>(T value) {
            return NonGeneric.GenericReturn(value);
        }

        [InterpreterTheory]
        [InlineData]
        public Generic<int> Generic_Result_LocalGenericType_NestedSystemType() {
            return NonGeneric.GenericReturnNestedLocal<int>();
        }
    }
}
