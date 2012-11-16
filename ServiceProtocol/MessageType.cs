using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceProtocol
{
    public enum MessageType : byte
    {
        /// <summary>
        /// Communication for EraS
        /// </summary>
        EraS = 0,

        /// <summary>
        /// 
        /// </summary>
        Internal = 1,

        /// <summary>
        /// Communication for service
        /// </summary>
        Service = 2,

        /// <summary>
        /// Control communication instances
        /// </summary>
        Control = 3,

        /// <summary>
        /// Communication to question origin
        /// </summary>
        Answer = 4,

        /// <summary>
        /// Communication to subscribers
        /// </summary>
        Public = 5,
    }
}
