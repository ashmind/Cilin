using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Cilin.Core;

namespace Cilin.Console {
    public class Program {
        public static int Main(string[] args) {
            var programPath = args[0];
            var program = AssemblyDefinition.ReadAssembly(programPath);
            var main = program.EntryPoint;

            var subArgs = args.Skip(1).ToArray();
            var result = new Interpreter(new Dictionary<AssemblyDefinition, string> {
                { program, programPath }
            }).InterpretCall(main, new[] { subArgs });
            return (result == null) ? 0 : (int)result;
        }
    }
}
