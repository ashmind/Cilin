using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            var module = ModuleDefinition.ReadModule(TestMethod.Module.FullyQualifiedName);
            var type = module.Types.Single(t => t.MetadataToken.ToInt32() == TestMethod.DeclaringType.MetadataToken);
            var method = type.Methods.Single(m => m.MetadataToken.ToInt32() == TestMethod.MetadataToken);

            var actualResult = TestMethod.Invoke(testClassInstance, TestMethodArguments);
            var interpretedResult = new Interpreter().InterpretCall(new TypeReference[0], method, new TypeReference[0], testClassInstance, TestMethodArguments);
            Assert.Equal(actualResult, interpretedResult);
            return null;
        }
    }
}
