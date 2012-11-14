using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeadlessClient.Protocols
{
    interface ICacheable<T>
    {
        T Key { get; }
    }
}