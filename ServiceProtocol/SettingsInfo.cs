using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;

namespace ServiceProtocol
{
    public class SettingsInfo : AbstractInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        public SettingsInfo(MessageClient client)
            : base("Settings", client)
        {

        }

        /// <summary>
        /// Gets the mongo address
        /// </summary>
        /// <returns></returns>
        public MongoServerAddress GetMongo()
        {
            var a = Client.AskQuestion(CreateQuestion("GetMongo"));
            return new MongoServerAddress(
                a.Packet.ReadString(),
                a.Packet.ReadInt32()
            );
        }
    }
}
