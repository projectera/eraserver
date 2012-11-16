using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using System.Threading.Tasks;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

namespace ERA.Services.Map.Data
{
    internal static class Tileset
    {
        private static Dictionary<ObjectId, ERA.Protocols.MapProtocol.Tileset> Cache = new Dictionary<ObjectId, ERA.Protocols.MapProtocol.Tileset>();

        /// <summary>
        /// Gets a tileset from the db
        /// </summary>
        /// <param name="id">id of tileset to get</param>
        /// <returns></returns>
        internal static Task<ERA.Protocols.MapProtocol.Tileset> Get(ObjectId id, Boolean tryCache = true)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(id, tryCache); });
        }

        /// <summary>
        /// Gets a tileset from the db
        /// </summary>
        /// <param name="name">name of tileset to get</param>
        /// <returns></returns>
        internal static Task<ERA.Protocols.MapProtocol.Tileset> Get(String name)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(name); });
        }

        /// <summary>
        /// Gets a tileset from the db, blocks while retrieving
        /// </summary>
        /// <param name="id">id of tileset to get</param>
        /// <returns></returns>
        internal static ERA.Protocols.MapProtocol.Tileset GetBlocking(ObjectId id, Boolean tryCache = true)
        {
            ERA.Protocols.MapProtocol.Tileset result = null;
            if (tryCache)
            {
                lock (Cache)
                {
                    if (Cache.TryGetValue(id, out result))
                        return result;
                }
            }

            result = GetCollection().FindOneById(id) as ERA.Protocols.MapProtocol.Tileset;

            lock (Cache) 
            {
                Cache.Remove(id);
                Cache.Add(id, result);
            }
            return result;
        }

        /// <summary>
        /// Gets a tileset from the db, blocks while retrieving
        /// </summary>
        /// <param name="name">name of tileset to get</param>
        /// <returns></returns>
        internal static ERA.Protocols.MapProtocol.Tileset GetBlocking(String name)
        {
            return GetCollection().FindOneAs<ERA.Protocols.MapProtocol.Tileset>(
                Query.Matches("Name", new BsonRegularExpression("^(?i)" + name + "$"))) as ERA.Protocols.MapProtocol.Tileset;
        }

        /// <summary>
        /// Gets the tilesets collection
        /// </summary>
        /// <returns></returns>
        internal static MongoCollection<ERA.Protocols.MapProtocol.Tileset> GetCollection()
        {
            return ServiceProtocol.ServiceClient.Database.GetCollection<ERA.Protocols.MapProtocol.Tileset>("Tilesets");
        }

        /// <summary>
        /// Puts a tileset to the db
        /// </summary>
        internal static void Put(ERA.Protocols.MapProtocol.Tileset tileset)
        {
            Put(tileset, SafeMode.False);
        }

        /// <summary>
        /// Puts a tileset to the db
        /// <param name="safemode">Sets the safemode on this query</param>
        /// </summary>
        internal static SafeModeResult Put(ERA.Protocols.MapProtocol.Tileset tileset, SafeMode safemode)
        {
            return GetCollection().Save<ERA.Protocols.MapProtocol.Tileset>(tileset, safemode);
        }

    }
}
