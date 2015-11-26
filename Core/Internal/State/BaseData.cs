using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cilin.Core.Internal.Reflection;

namespace Cilin.Core.Internal.State {
    public abstract class BaseData {
        private IDictionary<string, object> _fieldValues = null;

        public virtual object Get(InterpretedField field) {
            if (_fieldValues == null)
                _fieldValues = new Dictionary<string, object>();

            object value;
            if (!_fieldValues.TryGetValue(field.Name, out value)) {
                value = TypeSupport.GetDefaultValue(field.FieldType);
                _fieldValues.Add(field.Name, value);
            }

            return value;
        }

        public virtual void Set(InterpretedField field, object value) {
            if (_fieldValues == null)
                _fieldValues = new Dictionary<string, object>();

            _fieldValues[field.Name] = value;
        }
    }
}
