using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceProtocol
{
    public class SettingsInfo : AbstractInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        public SettingsInfo(ServiceClient client)
            : base("Settings", client)
        {

        }
    }
}
