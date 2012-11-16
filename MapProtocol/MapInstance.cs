using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace ERA.Protocols.MapProtocol
{
    public class MapInstance
    {
        /// <summary>
        /// Instance Id
        /// </summary>
        public ObjectId Id { get; protected set; }

        /// <summary>
        /// Map Id
        /// </summary>
        public ObjectId MapId { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="map"></param>
        public static MapInstance Generate(ObjectId map)
        {
            var result = new MapInstance();
            result.Id = ObjectId.GenerateNewId();
            result.MapId = map;
            return result;
        }
    }
}
