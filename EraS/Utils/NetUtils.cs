using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace EraS.Utils
{
    static class NetUtils
    {
        public static IPAddress GetIPAddress()
        {
            try
            {
                var web = new WebClient();
                var ip = web.DownloadString("http://ifconfig.me/ip").Trim();
                return IPAddress.Parse(ip);
            }
            catch (FormatException)
            {
                return IPAddress.None;
            }
            catch (WebException)
            {
                return IPAddress.None;
            }
        }
    }
}
