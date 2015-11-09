using System;
using System.Collections.ObjectModel;
using System.Reflection;
using Nylsor.Abstraction;
using Nylsor.Abstraction.ReflectionAdapters;
using Xunit;
using Xunit.Sdk;

namespace Nylsor.Tests.Support {
    public class DecompilerCaseCommand : TestCommand {
        private readonly IMethodInfo _method;
        private readonly IDecompilerTestTarget _target;
        private readonly ReadOnlyCollection<string> _expected;
        private readonly string _skip;

        public DecompilerCaseCommand(IMethodInfo method, IDecompilerTestTarget target, ReadOnlyCollection<string> expected, string skip)
            : base(method, target.Name, System.Threading.Timeout.Infinite)
        {
            _method = method;
            _target = target;
            _expected = expected;
            _skip = skip;
        }

        public override MethodResult Execute(object testClass) {
            if (_skip != null)
                return new SkipResult(_method, DisplayName, _skip);

            var methodInfo = _method.MethodInfo;
            var methodToDecompile = GetMethodToDecompile(methodInfo);
            
            var result = _target.DecompileToString(methodToDecompile);
            if (_expected.Count > 1) {
                Assert.Contains(result, _expected);
            }
            else {
                Assert.Equal(_expected[0], result);
            }
            return new PassedResult(_method, DisplayName); 
        }

        private static IMethod GetMethodToDecompile(MethodInfo methodInfo) {
            if (methodInfo.ReturnType != typeof(IMethod))
                return new ReflectionAdapterFactory().Adapt(methodInfo);

            // ReSharper disable once AssignNullToNotNullAttribute
            var instance = !methodInfo.IsStatic ? Activator.CreateInstance(methodInfo.ReflectedType) : null;
            return (IMethod)methodInfo.Invoke(instance, null);
        }

        public override bool ShouldCreateInstance {
            get { return false; }
        }
    }
}