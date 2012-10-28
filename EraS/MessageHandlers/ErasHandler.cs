using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Topography;
using ServiceProtocol;
using EraS.Connections;
using System.Threading.Tasks;

namespace EraS.MessageHandlers
{
    /// <summary>
    /// Handles EraS Messages
    /// </summary>
    class ErasHandler
    {
        public Network Network { get; set; }
        public Dictionary<String, Action<ServiceConnection, Message>> Functions { get; protected set; }

        public ErasHandler(Network network)
        {
            Network = network;
            Functions = new Dictionary<String, Action<ServiceConnection, Message>>();

            #region Register Functions
            Functions.Add("GetIdentifier", GetIdentifier);
            #endregion
        }

        public void HandleMessage(ServiceConnection c, Message m)
        {
            var function = m.Packet.ReadString();
            lock (Functions)
            {
                if (!Functions.ContainsKey(function))
                    return;
                Task.Factory.StartNew(() => Functions[function](c, m));
            }
        }

        protected void GetIdentifier(ServiceConnection c, Message m)
        {
            var ans = m.Answer(c);
            ans.Packet.Write(Network.Me.Identifier);
            c.SendMessage(ans);
        }
    }
}
