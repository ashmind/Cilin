using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Tests.Helpers;
using Xunit;

namespace Cilin.Tests {
    public class Variables {
        [InterpreterTheory]
        [InlineData]
        public int Many() {
            var a = 5;
            var b = a + 3;
            var c = 0;
            for (var i = 0; i < b; i++) {
                c += i;
            }
            var d = c * 2;
            var e = d * 3;
            var f = e / 2;

            return f;
        }
    }
}
