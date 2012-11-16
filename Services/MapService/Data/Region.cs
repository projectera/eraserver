using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

namespace ERA.Services.Map.Data
{
    public static class Region
    {
        /// <summary>
        /// Gets a region from the db
        /// </summary>
        /// <param name="id">id of region to get</param>
        /// <returns></returns>
        public static Task<ERA.Protocols.MapProtocol.Region> Get(ObjectId id)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(id); });
        }

        /// <summary>
        /// Gets a region from the db
        /// </summary>
        /// <param name="username">name of region to get</param>
        /// <returns></returns>
        public static Task<ERA.Protocols.MapProtocol.Region> Get(String name)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(name); });
        }

        /// <summary>
        /// Gets a region from the db, blocks while retrieving
        /// </summary>
        /// <param name="id">id of region to get</param>
        /// <returns></returns>
        public static ERA.Protocols.MapProtocol.Region GetBlocking(ObjectId id)
        {
            return GetCollection().FindOneById(id) as ERA.Protocols.MapProtocol.Region;
        }

        /// <summary>
        /// Gets a regions from the db, blocks while retrieving
        /// </summary>
        /// <param name="username">name of region to get</param>
        /// <returns></returns>
        public static ERA.Protocols.MapProtocol.Region GetBlocking(String name)
        {
            return GetCollection().FindOneAs<ERA.Protocols.MapProtocol.Region>(
                Query.Matches("Name", new BsonRegularExpression("^(?i)" + name + "$"))) as ERA.Protocols.MapProtocol.Region;
        }

        /// <summary>
        /// Gets the currency collection
        /// </summary>
        /// <returns></returns>
        public static MongoCollection<ERA.Protocols.MapProtocol.Region> GetCollection()
        {
            return ServiceProtocol.ServiceClient.Database.GetCollection<ERA.Protocols.MapProtocol.Region>("Regions");
        }

        /// <summary>
        /// Puts a region to the db
        /// </summary>
        public static void Put(ERA.Protocols.MapProtocol.Region region)
        {
            Put(region, SafeMode.False);
        }

        /// <summary>
        /// Puts a region to the db
        /// <param name="safemode">Sets the safemode on this query</param>
        /// </summary>
        public static SafeModeResult Put(ERA.Protocols.MapProtocol.Region region, SafeMode safemode)
        {
            return GetCollection().Save<ERA.Protocols.MapProtocol.Region>(region, safemode);
        }
    }
}
