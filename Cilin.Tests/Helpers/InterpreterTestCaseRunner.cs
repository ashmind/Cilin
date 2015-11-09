using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Cilin.Tests.Helpers {
    public class InterpreterTestCaseRunner : XunitTestCaseRunner {
        public InterpreterTestCaseRunner(
            IXunitTestCase testCase,
            string displayName,
            string skipReason,
            object[] constructorArguments,
            object[] testMethodArguments,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource
        ) : base(
            testCase,
            displayName,
            skipReason,
            constructorArguments,
            testMethodArguments,
            messageBus,
            aggregator,
            cancellationTokenSource
        ) {}

        protected override Task<RunSummary> RunTestAsync()
            => new InterpreterTestRunner(new XunitTest(TestCase, DisplayName), MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, SkipReason, BeforeAfterAttributes, new ExceptionAggregator(Aggregator), CancellationTokenSource).RunAsync();
    }
}
