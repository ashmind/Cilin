using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AshMind.Extensions;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Cilin.Tests.Helpers {
    [MeansImplicitUse]
    [XunitTestCaseDiscoverer("Cilin.Tests.Helpers.InterpreterTheoryDiscoverer", "Cilin.Tests")]
    public class InterpreterTheoryAttribute : FactAttribute {
    }
}
