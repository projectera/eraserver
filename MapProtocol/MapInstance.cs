using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace MapProtocol
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
        public MapInstance(ObjectId map)
        {
            Id = ObjectId.GenerateNewId();
            MapId = map;
        }
    }
}
