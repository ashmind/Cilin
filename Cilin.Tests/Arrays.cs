using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Tests.Helpers;
using Xunit;

namespace Cilin.Tests {
    public class Arrays {
        public class Class {
            public Class(string value) {
                Value = value;
            }

            public string Value { get; }
        }

        [InterpreterTheory]
        [InlineData(5)]
        [InlineData("x")]
        public T GetSetItem_SystemType<T>(T argument) {
            return new T[] { argument }[0];
        }

        [InterpreterTheory]
        [InlineData]
        public string GetSetItem_LocalType() {
            return new Class[] { new Class("x") }[0].Value;
        }

        [InterpreterTheory]
        [InlineData(5)]
        [InlineData("x")]
        public T Enumerate_SystemType<T>(T argument) {
            foreach (var item in (IEnumerable<T>)(new[] { argument })) {
                return item;
            }
            return default(T);
        }

        [InterpreterTheory]
        [InlineData]
        public string Enumerate_LocalType() {
            foreach (var item in (IEnumerable<Class>)(new[] { new Class("x") })) {
                return item.Value;
            }
            return null;
        }
    }
}
