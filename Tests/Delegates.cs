using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Tests.Helpers;
using Xunit;

namespace Cilin.Tests {
    public class Delegates {
        [InterpreterTheory]
        [InlineData]
        public int IdentityFunc() {
            return ((Func<int, int>)(x => x))(5);
        }

        [InterpreterTheory]
        [InlineData(5)]
        [InlineData(5.0)]
        [InlineData("x")]
        public T IdentityFunc_GenericArgument<T>(T argument) {
            return ((Func<T, T>)(x => x))(argument);
        }

        [InterpreterTheory]
        [InlineData]
        public int ClosureAction() {
            var x = 0;
            ((Action)(() => x = 5))();
            return x;
        }
    }
}
