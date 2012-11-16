using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ERA.Tools.HeadlessClient.Protocols
{
    interface ICacheable<T>
    {
        T Key { get; }
    }
}