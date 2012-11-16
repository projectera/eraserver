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
    internal static class Map
    {
        private static Dictionary<ObjectId, ERA.Protocols.MapProtocol.Map> Cache = new Dictionary<ObjectId, ERA.Protocols.MapProtocol.Map>();

        /// <summary>
        /// Gets a map from the db
        /// </summary>
        /// <param name="id">id of map to get</param>
        /// <returns></returns>
        internal static Task<ERA.Protocols.MapProtocol.Map> Get(ObjectId id, Boolean tryCache = true)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(id, tryCache); });
        }

        /// <summary>
        /// Gets a map from the db
        /// </summary>
        /// <param name="name">name of map to get</param>
        /// <returns></returns>
        internal static Task<ERA.Protocols.MapProtocol.Map> Get(String name)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(name); });
        }

        /// <summary>
        /// Gets a map from the db, blocks while retrieving
        /// </summary>
        /// <param name="id">id of map to get</param>
        /// <returns></returns>
        internal static ERA.Protocols.MapProtocol.Map GetBlocking(ObjectId id, Boolean tryCache = true)
        {
            ERA.Protocols.MapProtocol.Map result = null;
            if (tryCache)
            {
                lock (Cache)
                {
                    if (Cache.TryGetValue(id, out result))
                        return result;
                }
            }

            result = GetCollection().FindOneById(id) as ERA.Protocols.MapProtocol.Map;

            lock (Cache)
            {
                Cache.Remove(id);
                Cache.Add(id, result);
            }
            return result;
        }

        /// <summary>
        /// Gets a map from the db, blocks while retrieving
        /// </summary>
        /// <param name="name">name of map to get</param>
        /// <returns></returns>
        internal static ERA.Protocols.MapProtocol.Map GetBlocking(String name)
        {
            return GetCollection().FindOneAs<ERA.Protocols.MapProtocol.Map>(
                Query.Matches("Name", new BsonRegularExpression("^(?i)" + name + "$"))) as ERA.Protocols.MapProtocol.Map;
        }

        /// <summary>
        /// Gets the maps collection
        /// </summary>
        /// <returns></returns>
        internal static MongoCollection<ERA.Protocols.MapProtocol.Map> GetCollection()
        {
            return ServiceProtocol.ServiceClient.Database.GetCollection<ERA.Protocols.MapProtocol.Map>("Maps");
        }

        /// <summary>
        /// Puts a map to the db
        /// </summary>
        internal static void Put(ERA.Protocols.MapProtocol.Map map)
        {
            Put(map, SafeMode.False);
        }

        /// <summary>
        /// Puts a map to the db
        /// <param name="safemode">Sets the safemode on this query</param>
        /// </summary>
        internal static SafeModeResult Put(ERA.Protocols.MapProtocol.Map map, SafeMode safemode)
        {
            return GetCollection().Save<ERA.Protocols.MapProtocol.Map>(map, safemode);
        }
    }
}
