using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientProtocol
{
    public enum ClientProtocols : byte
    {
        /// <summary>
        /// The Extension Byte indicates that we should look at the ExtendedClientProtocols enum
        /// </summary>
        Extension = 0,

        /// <summary>
        /// The player protocol handles all Player related actions
        /// </summary>
        Player = 3,

        /// <summary>
        /// 
        /// </summary>
        Interactable = 4,

        /// <summary>
        /// 
        /// </summary>
        Guild = 5,

        /// <summary>
        /// 
        /// </summary>
        Team = 6,

        /// <summary>
        /// 
        /// </summary>
        Map = 7,

        /// <summary>
        /// Asset protocol handles all Asset related actions
        /// </summary>
        Asset = 8,
    }
}
