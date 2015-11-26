using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Tests.Helpers;
using Xunit;

namespace Cilin.Tests.Of.Reflection {
    public class Types {
        public interface Interface { }
        public class Class { }
        public struct Struct { }
        public class Generic<T> { }

        [InterpreterTheory]
        [InlineData(nameof(Interface))]
        [InlineData(nameof(Class))]
        public string BaseType(string typeName) {
            return typeof(Types).GetNestedType(typeName).BaseType?.Name;
        }

        [InterpreterTheory]
        [InlineData]
        public string AssemblyQualifiedName_GenericParameter() {
            return typeof(Generic<>).GetGenericArguments()[0].AssemblyQualifiedName;
        }

        [InterpreterTheory]
        [InlineData]
        public string FullName_Array() {
            return typeof(Class[]).FullName;
        }

        [InterpreterTheory]
        [InlineData]
        public string ElementType_Array() {
            return typeof(Class[]).GetElementType().FullName;
        }

        [InterpreterTheory]
        [InlineData]
        public TypeAttributes TypeAttributes_Array() {
            return typeof(Class[]).Attributes;
        }
    }
}
