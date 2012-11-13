using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using ProjectERA.Protocols;
using ERAUtils;

namespace ERAServer.Protocols.Client
{
    internal partial class Guild : Protocol
    {
        /// <summary>
        /// Finds a guild
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        internal Data.Guild Find(ObjectId search)
        {
            // Case 1: in cache
            Data.Guild data = Data.GeneralCache<ObjectId, Data.Guild>.QueryCache(search);
            if (data != null)
                return data;

            // Default: Looking for logged-out guild, fetch
            return Data.Guild.GetBlocking(search);
        }

        /// <summary>
        /// Invalidates this guild data
        /// </summary>
        /// <param name="local"></param>
        /// <param name="updated"></param>
        internal void Invalidate(Boolean local, Data.Guild updated)
        {
            // When locally called, push new data over protocol instances
            if (local)
            {
                updated = this.GuildData;
                Guild.Broadcast(this, (p, c) => ((Guild)p).Invalidate(false, updated));
            }
            // When remotely called, update data if match
            else if (updated.Id == this.Id)
            {
                this.GuildData = updated;
            }
        }
    }
}
