using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Tests.Helpers;
using Xunit;

namespace Cilin.Tests.Of.Reflection {
    public class Assemblies {
        [InterpreterTheory]
        [InlineData]
        public string GetExecutingAssembly() {
            return Assembly.GetExecutingAssembly().FullName;
        }
    }
}
