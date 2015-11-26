using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Core.Internal.Reflection {
    public class LazyMember<TMemberInfo> : ILazyMember<TMemberInfo>
        where TMemberInfo : MemberInfo
    {
        private readonly Lazy<TMemberInfo> _info;

        public LazyMember(string name, bool isStatic, bool isPublic, Func<TMemberInfo> resolve) {
            Name = name;
            IsStatic = isStatic;
            IsPublic = isPublic;
            MemberType = GetMemberType();
            _info = new Lazy<TMemberInfo>(resolve);
        }

        public string Name { get; }
        public bool IsStatic { get; }
        public bool IsPublic { get; }
        public MemberTypes MemberType { get; }
        public TMemberInfo Info => _info.Value;

        public MemberTypes GetMemberType() {
            if (typeof(TMemberInfo) == typeof(Type))
                return MemberTypes.NestedType;

            if (typeof(TMemberInfo) == typeof(ConstructorInfo))
                return MemberTypes.Constructor;

            if (typeof(TMemberInfo) == typeof(MethodInfo))
                return MemberTypes.Method;

            if (typeof(TMemberInfo) == typeof(FieldInfo))
                return MemberTypes.Field;

            if (typeof(TMemberInfo) == typeof(PropertyInfo))
                return MemberTypes.Property;

            if (typeof(TMemberInfo) == typeof(EventInfo))
                return MemberTypes.Event;

            // not accessible
            return (MemberTypes)(-1);
        }

        public override string ToString() => $"Lazy<{GetMemberType()} {Name}>";
    }
}
