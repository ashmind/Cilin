using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.State;
using Mono.Cecil;

using MethodAttributes = System.Reflection.MethodAttributes;
using MethodImplAttributes = System.Reflection.MethodImplAttributes;

namespace Cilin.Internal.Reflection {
    public class InterpretedMethod : MethodInfo {
        private readonly InterpretedType _declaringType;
        private readonly string _name;
        private readonly Type _returnType;
        private readonly ParameterInfo[] _parameters;
        private readonly MethodAttributes _attributes;
        private readonly Func<object, object[], object> _invoke;

        public InterpretedMethod(
            InterpretedType declaringType,
            string name,
            Type returnType,
            ParameterInfo[] parameters,
            MethodAttributes attributes,
            Func<object, object[], object> invoke
        ) {
            _declaringType = declaringType;
            _name = name;
            _returnType = returnType;
            _parameters = parameters;
            _attributes = attributes;
            _invoke = invoke;
        }

        public override MethodAttributes Attributes => _attributes;
        public override Type DeclaringType => _declaringType;

        public override RuntimeMethodHandle MethodHandle {
            get { return Delegator.GetMethod(this).MethodHandle; }
        }

        public override string Name => _name;

        public override Type ReflectedType {
            get {
                throw new NotImplementedException();
            }
        }

        public override System.Reflection.ICustomAttributeProvider ReturnTypeCustomAttributes {
            get {
                throw new NotImplementedException();
            }
        }

        public override MethodInfo GetBaseDefinition() {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(bool inherit) {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }

        public override MethodImplAttributes GetMethodImplementationFlags() {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters() {
            return _parameters;
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            if (IsStatic)
                return InvokeNonVirtual(obj, invokeAttr, binder, parameters, culture);

            return ((CilinObject)ObjectWrapper.UnwrapIfRequired(obj))
                .Invoke(this, invokeAttr, binder, parameters, culture);
        }

        public object InvokeNonVirtual(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
            _declaringType.EnsureStaticConstructorRun();
            return _invoke(obj, parameters);
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }

        public override Type ReturnType {
            get { return _returnType; }
        }
        
        private class Delegator {
            private static readonly IList<MethodInfo> _actions = new List<MethodInfo>();
            private static readonly IList<MethodInfo> _funcs = new List<MethodInfo>();
            
            static Delegator() {
                var methods = typeof(Delegator).GetMethods(BindingFlags.Instance | BindingFlags.Public);
                _actions = methods.Where(m => m.Name.StartsWith("Action")).OrderBy(m => m.GetParameters().Length).ToArray();
                _funcs = methods.Where(m => m.Name.StartsWith("Func")).OrderBy(m => m.GetParameters().Length).ToArray();
            }
            
            public static MethodInfo GetMethod(InterpretedMethod method) {
                var parameters = method.GetParameters();
                var types = parameters.Select(p => Erase(p.ParameterType)).ToList();
                if (method.ReturnType == typeof(void))
                    return _actions[parameters.Length].MakeGenericMethod(types.ToArray());

                types.Add(Erase(method.ReturnType));
                return _funcs[parameters.Length].MakeGenericMethod(types.ToArray());
            }

            private static Type Erase(Type type) {
                return (type is InterpretedType) ? typeof(CilinObject) : type;
            }

            public void Action() {}
            public void Action<TArg1>(TArg1 arg1) {}
            public TResult Func<TResult>() { return default(TResult); }
            public TResult Func<TArg1, TResult>(TArg1 arg1) { return default(TResult); }
            public TResult Func<TArg1, TArg2, TResult>(TArg1 arg1, TArg2 arg2) { return default(TResult); }
        }
    }
}
