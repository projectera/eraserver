using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using System.Threading.Tasks;
using Lidgren.Network;

namespace ERA.Services.Map.Data
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
        /// Action Queue for this instance
        /// </summary>
        protected ActionQueue Queue { get; set; }

        /// <summary>
        /// Creates a new map Instance
        /// </summary>
        /// <param name="data"></param>
        protected MapInstance(MapProtocol.Map data)
        {
            this.MapData = data;
            this.InstanceData = MapProtocol.MapInstance.Generate(data.Id);

            this.Queue = new ActionQueue();
        }

        /// <summary>
        /// Starts a new instance
        /// </summary>
        /// <param name="map">On what map</param>
        /// <returns>The new instance</returns>
        public static MapInstance StartInstance(MapProtocol.Map map)
        {
            var result = new MapInstance(map);
            Program.MapInstances.AddInside(result.InstanceData.MapId, result.InstanceData.Id, result);
            Program.MapSubscriptions.AddSubscriptionList(map.Id.ToString());
            return result;
        }
    }
}
