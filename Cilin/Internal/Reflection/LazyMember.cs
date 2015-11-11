using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.Reflection {
    public abstract class LazyMember {
        public LazyMember(string name, bool isStatic) {
            Name = name;
            IsStatic = isStatic;
        }

        public string Name { get; }
        public bool IsStatic { get; }
    }

    public class LazyMember<TMemberInfo> : LazyMember 
        where TMemberInfo : MemberInfo
    {
        private readonly Lazy<TMemberInfo> _info;

        public LazyMember(string name, bool isStatic, Func<TMemberInfo> resolve)
            : base(name, isStatic)
        {
            _info = new Lazy<TMemberInfo>(resolve);
        }

        public TMemberInfo Info {
            get { return _info.Value; }
        }
    }
}
