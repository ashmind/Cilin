using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AshMind.Extensions;
using Mono.Cecil;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Cilin.Tests.Helpers {
    public class InterpreterTestInvoker : XunitTestInvoker {
        public InterpreterTestInvoker(
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource
        ) : base(
            test,
            messageBus,
            testClass,
            constructorArguments,
            testMethod,
            testMethodArguments,
            beforeAfterAttributes,
            aggregator,
            cancellationTokenSource
        ) {}

        protected override object CallTestMethod(object testClassInstance) {
            var modules = new Dictionary<string, ModuleDefinition>();
            var testType = ResolveType(TestMethod.DeclaringType, modules);
            var testMethod = testType.Methods.Single(m => m.MetadataToken.ToInt32() == TestMethod.MetadataToken);
            var methodTypeArguments = TestMethod.GetGenericArguments().Select(t => ResolveType(t, modules)).ToArray();

            var actualResult = TestMethod.Invoke(testClassInstance, TestMethodArguments);
            var interpretedResult = new Interpreter().InterpretCall(new TypeReference[0], testMethod, methodTypeArguments, testClassInstance, TestMethodArguments);
            Assert.Equal(actualResult, interpretedResult);
            return null;
        }

        private TypeDefinition ResolveType(Type type, IDictionary<string, ModuleDefinition> modules) {
            var module = modules.GetOrAdd(type.Module.FullyQualifiedName, name => ModuleDefinition.ReadModule(name));
            return module.Types.Single(t => t.MetadataToken.ToInt32() == TestMethod.DeclaringType.MetadataToken);
        }
    }
}
