using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Core.Internal {
    public static class Primitives {
        public static bool NotEqual(Stack<object> stack) => !Equal(stack);

        public static bool Equal(Stack<object> stack) {
            var right = stack.Pop();
            var left = stack.Pop();
            return Equal(left, right);
        }

        public static bool Equal(object left, object right) {
            return object.Equals(left, right);
        }

        public static bool IsGreaterThan(object left, object right) {
            if (left == null)
                return false;

            if (right == null)
                return true;

            return (int)left > (int)right;
        }

        public static bool IsLessThan(object left, object right) {
            if (right == null)
                return false;

            if (left == null)
                return true;

            if (left is int) {
                if (right is int)
                    return (int)left < (int)right;

                if (right is sbyte)
                    return (int)left < (sbyte)right;
            }

            throw new NotImplementedException();
        }

        public static object Add(object left, object right) {
            return (int)left + (int)right;
        }

        public static object Divide(object left, object right) {
            return (int)left / (int)right;
        }

        public static object Multiply(object left, object right) {
            return (int)left * (int)right;
        }

        public static object Convert(object value, Type targetType) {
            if (value?.GetType() == targetType)
                return value;

            if (targetType == typeof(int)) {
                if (value is sbyte)
                    return (int)(sbyte)value;

                if (value is short)
                    return (int)(short)value;

                if (value is long)
                    return (int)(long)value;

                if (value is byte)
                    return (int)(byte)value;

                if (value is ushort)
                    return (int)(ushort)value;

                if (value is uint)
                    return (int)(uint)value;

                if (value is ulong)
                    return (int)(ulong)value;

                if (value is IntPtr)
                    return (int)(IntPtr)value;
            }

            if (targetType == typeof(IntPtr)) {
                if (value is sbyte)
                    return (IntPtr)(sbyte)value;

                if (value is short)
                    return (IntPtr)(short)value;

                if (value is int)
                    return (IntPtr)(int)value;

                if (value is long)
                    return (IntPtr)(long)value;

                if (value is byte)
                    return (IntPtr)(byte)value;

                if (value is ushort)
                    return (IntPtr)(ushort)value;

                if (value is uint)
                    return (IntPtr)(uint)value;

                if (value is ulong)
                    return (IntPtr)(ulong)value;
            }

            throw new NotImplementedException($"Conversion from {value.GetType()} (value {value}) to type {targetType} is not implemented.");
        }
    }
}
