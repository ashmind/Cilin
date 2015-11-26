using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Core.Internal.Reflection;

namespace Cilin.Core.Internal.State {
    public class StaticData : BaseData {
        private InterpretedType _owner;

        public StaticData(InterpretedType owner) {
            _owner = owner;
        }

        public override object Get(InterpretedField field) {
            _owner.EnsureStaticConstructorRun();
            return base.Get(field);
        }

        public override void Set(InterpretedField field, object value) {
            _owner.EnsureStaticConstructorRun();
            base.Set(field, value);
        }
    }
}
