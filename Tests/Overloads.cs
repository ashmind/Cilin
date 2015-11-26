using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cilin.Tests.Helpers;
using Xunit;

namespace Cilin.Tests {
    public class Overloads {
        public class Base {
            public virtual string M(int x) => "base-int-1";
            public virtual string M(int x, int y) => "base-int-2";
            public virtual string M(string x) => "base-string";
        }

        public class Subclass : Base {
            public override string M(int x) => "subclass-int-1";
        }

        [InterpreterTheory]
        [InlineData]
        private string Subclass_Method() {
            var s = (Base)new Subclass();
            return s.M(5);
        }

        [InterpreterTheory]
        [InlineData]
        private string Base_Method() {
            var s = (Base)new Subclass();
            return s.M(5, 5);
        }
    }
}
