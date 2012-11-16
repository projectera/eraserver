using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using System.Threading.Tasks;
using Lidgren.Network;
using ERA.Utils;
using ERA.Services.InteractableService;
using ERA.Services.InteractableService.Data;

namespace ERA.Services.MapService.Data
{
    public class MapInteractablesInstance
    {
        /// <summary>
        /// Map Data
        /// </summary>
        public ERA.Protocols.MapProtocol.MapInstance InstanceData { get; protected set; }

        /// <summary>
        /// Map Data
        /// </summary>
        public ERA.Protocols.MapProtocol.Map MapData { get; protected set; }

        /// <summary>
        /// When was the instance created
        /// </summary>
        public DateTime CreationTime { get { return InstanceData.Id.CreationTime.ToUniversalTime(); } }

        /// <summary>
        /// Action Queue for this instance
        /// </summary>
        protected ActionQueue Queue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        protected  Dictionary<ObjectId, ServerInteractable> Interactables { get; set; }

        /// <summary>
        /// Creates a new map Instance
        /// </summary>
        /// <param name="data"></param>
        protected MapInteractablesInstance(ERA.Protocols.MapProtocol.Map map, ERA.Protocols.MapProtocol.MapInstance data)
        {
            this.MapData = map;
            this.InstanceData = data;
            this.Interactables = new Dictionary<ObjectId, ServerInteractable>();

            this.Queue = new ActionQueue();
        }

        /// <summary>
        /// Starts a new instance
        /// </summary>
        /// <param name="map">On what map</param>
        /// <param name="data">On what map instance</param>
        /// <returns>The new instance</returns>
        public static MapInteractablesInstance StartInstance(ERA.Protocols.MapProtocol.Map map, ERA.Protocols.MapProtocol.MapInstance data)
        {
            var result = new MapInteractablesInstance(map, data);
            Program.MapInteractablesInstances.AddInside(result.InstanceData.MapId, result.InstanceData.Id, result);
            foreach (var interactable in result.Interactables.Keys)
                Program.InteractableSubscriptions.AddSubscriptionList(interactable.ToString());
            return result;
        }

        //public StartInteractable(Data.Interactable interactable)
    }
}
