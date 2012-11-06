using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using System.Threading.Tasks;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

namespace MapService.Data
{
    internal static class Tileset
    {
        private static Dictionary<ObjectId, MapProtocol.Tileset> Cache = new Dictionary<ObjectId, MapProtocol.Tileset>();

        /// <summary>
        /// Gets a tileset from the db
        /// </summary>
        /// <param name="id">id of tileset to get</param>
        /// <returns></returns>
        internal static Task<MapProtocol.Tileset> Get(ObjectId id, Boolean tryCache = true)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(id, tryCache); });
        }

        /// <summary>
        /// Gets a tileset from the db
        /// </summary>
        /// <param name="name">name of tileset to get</param>
        /// <returns></returns>
        internal static Task<MapProtocol.Tileset> Get(String name)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(name); });
        }

        /// <summary>
        /// Gets a tileset from the db, blocks while retrieving
        /// </summary>
        /// <param name="id">id of tileset to get</param>
        /// <returns></returns>
        internal static MapProtocol.Tileset GetBlocking(ObjectId id, Boolean tryCache = true)
        {
            MapProtocol.Tileset result = null;
            if (tryCache)
            {
                lock (Cache)
                {
                    if (Cache.TryGetValue(id, out result))
                        return result;
                }
            }

            result = GetCollection().FindOneById(id) as MapProtocol.Tileset;

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
        internal static MapProtocol.Tileset GetBlocking(String name)
        {
            return GetCollection().FindOneAs<MapProtocol.Tileset>(
                Query.Matches("Name", new BsonRegularExpression("^(?i)" + name + "$"))) as MapProtocol.Tileset;
        }

        /// <summary>
        /// Gets the tilesets collection
        /// </summary>
        /// <returns></returns>
        internal static MongoCollection<MapProtocol.Tileset> GetCollection()
        {
            return ServiceProtocol.ServiceClient.Database.GetCollection<MapProtocol.Tileset>("Tilesets");
        }

        /// <summary>
        /// Puts a tileset to the db
        /// </summary>
        internal static void Put(MapProtocol.Tileset tileset)
        {
            Put(tileset, SafeMode.False);
        }

        /// <summary>
        /// Puts a tileset to the db
        /// <param name="safemode">Sets the safemode on this query</param>
        /// </summary>
        internal static SafeModeResult Put(MapProtocol.Tileset tileset, SafeMode safemode)
        {
            return GetCollection().Save<MapProtocol.Tileset>(tileset, safemode);
        }

    }
}
