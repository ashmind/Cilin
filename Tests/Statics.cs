using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Cilin.Tests.Helpers;

namespace Cilin.Tests {
    public class Statics {
        private static class ClassWithStaticConstructor {
            public static readonly int Field;
            static ClassWithStaticConstructor() {
                Field = 5;
            }
        }

        private static class GenericWithStaticConstructor<T> {
            public static readonly string Field;
            static GenericWithStaticConstructor() {
                Field = typeof(T).Name;
            }
        }

        [InterpreterTheory]
        [InlineData]
        public int StaticConstructor_Explicit() {
            return ClassWithStaticConstructor.Field;
        }

        [InterpreterTheory]
        [InlineData]
        public string StaticConstructor_Generic_SystenType() {
            return GenericWithStaticConstructor<string>.Field
                 + GenericWithStaticConstructor<int>.Field;
        }
    }
}
