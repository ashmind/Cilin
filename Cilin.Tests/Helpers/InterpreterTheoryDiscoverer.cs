using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Cilin.Tests.Helpers {
    public class InterpreterTheoryDiscoverer : TheoryDiscoverer {
        private readonly IMessageSink _diagnosticMessageSink;

        public InterpreterTheoryDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink) {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override IXunitTestCase CreateTestCaseForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute) {
            throw new NotImplementedException();
        }

        protected override IXunitTestCase CreateTestCaseForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object[] dataRow)
            => new InterpreterTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, dataRow);
    }
}
