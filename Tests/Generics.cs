using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cilin.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cilin.Tests {
    public class Generics {
        public class LocalClass { }
        public class GenericBase<T> {}
        public class Generic<T> : GenericBase<T> {
            public static T Field;

            public static void GenericVoid<U>() { }
            public static T ReturnFromType(T value) { return value; }
        }

        public static class NonGeneric {
            public static void GenericVoid<T>() { }
            public static T GenericReturn<T>(T value) { return value; }
            public static Generic<T> GenericReturnNestedLocal<T>() { return null; }
        }

        public class Enumerable<T> : IEnumerable<T> {
            public IEnumerator<T> GetEnumerator() { throw new NotSupportedException(); }
            IEnumerator IEnumerable.GetEnumerator() { throw new NotSupportedException(); }
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
        public Generic<int> GenericMethod_LocalTypeNestedInRuntimeType() {
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

        [InterpreterTheory]
        [InlineData(5)]
        public bool GenericClass_Cast_BaseType<T>(T value) {
            var instance = new Generic<T>();
            var @base = (GenericBase<T>)instance;
            return @base != null;
        }

        [InterpreterTheory]
        [InlineData(5)]
        public bool GenericClass_Cast_RuntimeInterface<T>(T value) {
            var instance = new Enumerable<T>();
            var Runtime = (IEnumerable<T>)instance;
            return Runtime != null;
        }
    }
}
