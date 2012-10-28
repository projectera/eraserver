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

        protected void GetConnectedServers(ServiceConnection c, Message m)
        {
            var ans = m.Answer(c);
            lock (Network)
            {
                m.Packet.Write(Network.Servers.Count);
                foreach (var s in Network.Servers.Values)
                    m.Packet.Write(s.Identifier);
            }
            c.SendMessage(ans);
        }

        protected void GetServerServices(ServiceConnection c, Message m)
        {
            var id = m.Packet.ReadString();
            lock (Network)
            {
                if(!Network.Servers.ContainsKey(id))
                    return;

                Server s = Network.Servers[id];
                var ans = m.Answer(c);
                ans.Packet.Write(s.Services.Count);

            }
        }
    }
}
