using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal {
    public static class PrimitiveOperations {
        public static bool NotEqual(Stack<object> stack) {
            return !Equal(stack);
        }

        public static bool Equal(Stack<object> stack) {
            var right = stack.Pop();
            var left = stack.Pop();
            return Equals(left, right);
        }
    }
}
