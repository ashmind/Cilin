using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Core.Internal.Reflection {
    public interface ILazyMember<out TMemberInfo> {
        bool IsPublic { get; }
        bool IsStatic { get; }
        string Name { get; }
        TMemberInfo Info { get; }
        MemberTypes MemberType { get; }
    }
}
