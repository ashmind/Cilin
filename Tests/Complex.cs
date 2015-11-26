using System;
using System.Linq;
using Cilin.Tests.Helpers;
using Xunit;

namespace Cilin.Tests {
    public class Complex {
        [InterpreterTheory]
        [InlineData]
        public string PersonFirstByCondition() {
            var people = new[] {
                new Person { Name = "A" },
                new Person { Name = "B" },
                new Person { Name = "C" }
            };

            return people
                .Where(p => p.Name == "B")
                .Select(p => p.Name)
                .FirstOrDefault();
        }

        private class Person {
            public string Name { get; set; }
        }
    }
}
