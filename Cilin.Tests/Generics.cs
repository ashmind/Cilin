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
            public static T Field;

            public static void GenericVoid<U>() { }
            public static T ReturnFromType(T value) { return value; }
        }

        public static class NonGeneric {
            public static void GenericVoid<T>() { }
            public static T GenericReturn<T>(T value) { return value; }
            public static Generic<T> GenericReturnNestedLocal<T>() { return null; }
        }

        [InterpreterTheory]
        [InlineData]
        public void GenericVoidMethod_LocalType() {
            NonGeneric.GenericVoid<LocalClass>();
        }

        [InterpreterTheory]
        [InlineData]
        public void GenericVoidMethod_RuntimeType() {
            NonGeneric.GenericVoid<int>();
        }

        [InterpreterTheory]
        [InlineData]
        public int GenericMethod_TypeDefinedByArgument() {
            return NonGeneric.GenericReturn(5);
        }

        [InterpreterTheory]
        [InlineData(5)]
        [InlineData("x")]
        public T GenericMethod_TypeDefinedByTestMethod<T>(T value) {
            return NonGeneric.GenericReturn(value);
        }

        [InterpreterTheory]
        [InlineData]
        public Generic<int> GenericMethod_LocalTypeNestedInSystemType() {
            return NonGeneric.GenericReturnNestedLocal<int>();
        }

        [InterpreterTheory]
        [InlineData]
        public void GenericClass_GenericVoidMethod_LocalType() {
            Generic<LocalClass>.GenericVoid<LocalClass>();
        }

        [InterpreterTheory]
        [InlineData(5)]
        [InlineData("x")]
        public T GenericClass_NonGenericMethod_TypeDefinedByTestMethod<T>(T value) {
            return Generic<T>.ReturnFromType(value);
        }

        [InterpreterTheory]
        [InlineData(5)]
        [InlineData("x")]
        public T GenericClass_Field_TypeDefinedByTestMethod<T>(T value) {
            Generic<T>.Field = value;
            return Generic<T>.Field;
        }
    }
}
