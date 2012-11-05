using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace MapService.Data
{
    public class MapInstance
    {
        /// <summary>
        /// Instance Data
        /// </summary>
        public MapProtocol.MapInstance InstanceData { get; protected set; }

        /// <summary>
        /// Map Data
        /// </summary>
        public MapProtocol.Map MapData { get; protected set; }

        /// <summary>
        /// When was the instance created
        /// </summary>
        public DateTime CreationTime { get { return InstanceData.Id.CreationTime.ToUniversalTime(); } }

        /// <summary>
        /// Creates a new map Instance
        /// </summary>
        /// <param name="data"></param>
        protected MapInstance(MapProtocol.Map data)
        {
            MapData = data;
            InstanceData = new MapProtocol.MapInstance(data.Id); 
        }

        /// <summary>
        /// Starts a new instance
        /// </summary>
        /// <param name="map">On what map</param>
        /// <returns>The new instance</returns>
        public static MapInstance StartInstance(MapProtocol.Map map)
        {
            var result = new MapInstance(map);

            lock (Program.MapInstances)
            {
                Dictionary<ObjectId, MapInstance> instances;
                if (!Program.MapInstances.TryGetValue(result.InstanceData.MapId, out instances)) {
                    instances = new Dictionary<ObjectId,MapInstance>();
                    Program.MapInstances.Add(result.InstanceData.MapId, instances);
                }
                
                instances.Add(result.InstanceData.Id, result);
            }

            return result;
        }
    }
}
