using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal.Reflection {
    public abstract class NonRuntimeType : Type {
        private NonRuntimeHandle _handle;

        protected NonRuntimeType() {

        }

        public new NonRuntimeHandle TypeHandle {
            get {
                if (_handle == null)
                    _handle = new NonRuntimeHandle(this);

                return _handle;
            }
        }
    }
}
