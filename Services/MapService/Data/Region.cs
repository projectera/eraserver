using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

namespace MapService.Data
{
    public static class Region
    {
        /// <summary>
        /// Gets a region from the db
        /// </summary>
        /// <param name="id">id of region to get</param>
        /// <returns></returns>
        public static Task<MapProtocol.Region> Get(ObjectId id)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(id); });
        }

        /// <summary>
        /// Gets a region from the db
        /// </summary>
        /// <param name="username">name of region to get</param>
        /// <returns></returns>
        public static Task<MapProtocol.Region> Get(String name)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(name); });
        }

        /// <summary>
        /// Gets a region from the db, blocks while retrieving
        /// </summary>
        /// <param name="id">id of region to get</param>
        /// <returns></returns>
        public static MapProtocol.Region GetBlocking(ObjectId id)
        {
            return GetCollection().FindOneById(id) as MapProtocol.Region;
        }

        /// <summary>
        /// Gets a regions from the db, blocks while retrieving
        /// </summary>
        /// <param name="username">name of region to get</param>
        /// <returns></returns>
        public static MapProtocol.Region GetBlocking(String name)
        {
            return GetCollection().FindOneAs<MapProtocol.Region>(
                Query.Matches("Name", new BsonRegularExpression("^(?i)" + name + "$"))) as MapProtocol.Region;
        }

        /// <summary>
        /// Gets the currency collection
        /// </summary>
        /// <returns></returns>
        public static MongoCollection<MapProtocol.Region> GetCollection()
        {
            return Program.Database.GetCollection<MapProtocol.Region>("Regions");
        }

        /// <summary>
        /// Puts a region to the db
        /// </summary>
        public static void Put(MapProtocol.Region region)
        {
            Put(region, SafeMode.False);
        }

        /// <summary>
        /// Puts a region to the db
        /// <param name="safemode">Sets the safemode on this query</param>
        /// </summary>
        public static SafeModeResult Put(MapProtocol.Region region, SafeMode safemode)
        {
            return GetCollection().Save<MapProtocol.Region>(region, safemode);
        }
    }
}
