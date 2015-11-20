using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AshMind.Extensions;
using Cilin.Internal.State;
using Mono.Cecil;

namespace Cilin.Internal.Reflection {
    public abstract class InterpretedType : NonRuntimeType {
        private bool _staticConstructorStarted;
        private readonly Lazy<Type> _baseType;
        private readonly Lazy<Type[]> _interfaces;
        private readonly IReadOnlyCollection<LazyMember> _members;

        private Lazy<string> _fullName;
        private Lazy<string> _assemblyQualifiedName;

        public StaticData StaticData { get; }

        public InterpretedType(
            Lazy<Type> baseType,
            Lazy<Type[]> interfaces,
            Func<Type, IReadOnlyCollection<LazyMember>> getMembers
        ) {
            _baseType = baseType;
            _interfaces = interfaces;
            _members = getMembers(this);

            _fullName = new Lazy<string>(GetFullName);
            _assemblyQualifiedName = new Lazy<string>(GetAssemblyQualifiedName);

            StaticData = new StaticData(this);
        }

        public override string AssemblyQualifiedName => _assemblyQualifiedName.Value;
        public override string FullName => _fullName.Value;

        public override Type BaseType => _baseType.Value;
        public override Type UnderlyingSystemType => this;

        public void EnsureStaticConstructorRun() {
            if (ContainsGenericParameters)
                throw new NotSupportedException($"Attempted to call static constructor on open generic type {FullName}.");

            if (_staticConstructorStarted)
                return;

            _staticConstructorStarted = true;

            var constructors = GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (constructors.Length == 0)
                return;

            constructors[0].Invoke(null);
        }

        public override Type[] GetInterfaces() => _interfaces.Value;

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => GetMembers<ConstructorInfo>(bindingAttr);
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => GetMembers<MethodInfo>(bindingAttr);

        public override MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria) {
            if (filter != FilterName)
                return base.FindMembers(memberType, bindingAttr, filter, filterCriteria);

            var results = new List<MemberInfo>();
            foreach (var member in _members) {
                if (member.Name != (string)filterCriteria)
                    continue;

                if (!MemberMatches(member, bindingAttr))
                    continue;

                if ((memberType & member.GetMemberType()) == 0)
                    continue;

                results.Add(member.InfoUntyped);
            }

            return results.ToArray();
        }

        private T[] GetMembers<T>(BindingFlags bindingAttr)
            where T : MemberInfo
        {
            return _members
                .OfType<LazyMember<T>>()
                .Where(m => MemberMatches(m, bindingAttr))
                .Select(m => m.Info)
                .ToArray();
        }

        private bool MemberMatches(LazyMember member, BindingFlags bindingAttr) {
            var unsupported = BindingFlags.DeclaredOnly
                            | BindingFlags.ExactBinding
                            | BindingFlags.FlattenHierarchy
                            | BindingFlags.GetField
                            | BindingFlags.GetProperty
                            | BindingFlags.IgnoreCase
                            | BindingFlags.IgnoreReturn
                            | BindingFlags.InvokeMethod
                            | BindingFlags.OptionalParamBinding
                            | BindingFlags.PutDispProperty
                            | BindingFlags.PutRefDispProperty
                            | BindingFlags.SetField
                            | BindingFlags.SetProperty
                            | BindingFlags.SuppressChangeType;
            if ((bindingAttr & unsupported) != 0)
                throw new NotImplementedException();

            if (bindingAttr == BindingFlags.Default)
                bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

            if ((bindingAttr & BindingFlags.Public) == 0 && member.IsPublic)
                return false;

            if ((bindingAttr & BindingFlags.NonPublic) == 0 && !member.IsPublic)
                return false;

            if ((bindingAttr & BindingFlags.Instance) == 0 && !member.IsStatic)
                return false;

            if ((bindingAttr & BindingFlags.Static) == 0 && member.IsStatic)
                return false;

            return true;
        }

        protected abstract string GetFullName();

        private string GetAssemblyQualifiedName() => FullName + ", " + Assembly.FullName;
        public override string ToString() => FullName;
    }
}
