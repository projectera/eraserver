using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using MongoDB.Bson;
using ERAServer.Services.Listeners;

namespace ERAServer.Protocols.Server.Misc
{
    internal partial class Misc : Protocol
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        private void MapBroadcast(NetIncomingMessage msg)
        {
            ObjectId mapId = new ObjectId(msg.ReadBytes(12));
            Servers.MapConnectionMapping.AddOrUpdate(mapId, this.Connection, (i, c) => this.Connection);
        }
    }
}
