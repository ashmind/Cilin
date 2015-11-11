using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cilin.Internal {
    public static class Empty<T> {
        public static T[] Array { get; } = new T[0];
    }
}
