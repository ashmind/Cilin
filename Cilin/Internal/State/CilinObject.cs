using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Internal.Reflection;

namespace Cilin.Internal.State {
    public class CilinObject : BaseData, ITypeOverride {
        public CilinObject(InterpretedType objectType) {
            ObjectType = objectType;
        }

        public InterpretedType ObjectType { get; }

        Type ITypeOverride.Type {
            get { return ObjectType; }
        }
    }
}
