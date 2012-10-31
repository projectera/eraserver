using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Connections;
using ServiceProtocol;
using EraS.Services;

namespace EraS.MessageHandlers.ErasComponents
{
    class SettingsComponent : DefaultComponent
    {
        public SettingsComponent() : base("Settings")
        {
            Functions.Add("GetMongo", GetMongo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        protected void GetMongo(MessageClient c, Message m)
        {
            var ans = m.Answer(c);
            ans.Packet.Write(HeartBeatService.ServerAddress.Host);
            ans.Packet.Write(HeartBeatService.ServerAddress.Port);
            c.SendMessage(ans);
        }
    }
}
