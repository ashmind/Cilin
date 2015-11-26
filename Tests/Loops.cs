using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Tests.Helpers;
using Xunit;

namespace Cilin.Tests {
    public class Loops {
        [InterpreterTheory]
        [InlineData]
        public int For() {
            var result = 0;
            for (var i = 0; i < 20; i++) {
                result = i;
            }
            return result;
        }
    }
}
