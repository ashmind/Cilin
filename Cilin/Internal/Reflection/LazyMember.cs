using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.Reflection {
    public abstract class LazyMember {
        public LazyMember(string name, bool isStatic, bool isPublic) {
            Name = name;
            IsStatic = isStatic;
            IsPublic = isPublic;
        }

        public string Name { get; }
        public bool IsStatic { get; }
        public bool IsPublic { get; }
        public abstract MemberInfo InfoUntyped { get; }
        public abstract MemberTypes GetMemberType();

        public override string ToString() => $"Lazy<{GetMemberType()} {Name}>";
    }

    public class LazyMember<TMemberInfo> : LazyMember 
        where TMemberInfo : MemberInfo
    {
        private readonly Lazy<TMemberInfo> _info;

        public LazyMember(string name, bool isStatic, bool isPublic, Func<TMemberInfo> resolve)
            : base(name, isStatic, isPublic)
        {
            _info = new Lazy<TMemberInfo>(resolve);
        }

        public TMemberInfo Info => _info.Value;
        public override MemberInfo InfoUntyped => Info;
        public override MemberTypes GetMemberType() {
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
    }
}
