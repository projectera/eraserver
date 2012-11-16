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
using ERA.Protocols.ServiceProtocol;

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

            var question = Program.EraSClient.CreateQuestion(Protocols.ServiceProtocol.MessageType.Service, "Map");
            question.Packet.Write("Subscribe");
            question.Packet.Write(result.InstanceData.Id.ToString());
            var answer = Program.EraSClient.AskQuestion(question);
            if (answer == null)
            {
                Console.WriteLine("Could not subscribe on map service to {0}:{1}", result.InstanceData.MapId, result.InstanceData.Id);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destination"></param>
        public void TransferInstance(String destination)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Stops running this instance
        /// </summary>
        public void StopInstance()
        {
            var question = Program.EraSClient.CreateQuestion(Protocols.ServiceProtocol.MessageType.Service, "Map");
            question.Packet.Write("Unsubscribe");
            question.Packet.Write(InstanceData.Id.ToString());
            var answer = Program.EraSClient.AskQuestion(question);
            if (answer == null)
            {
                Console.WriteLine("Could not usubscribe on map service from {0}:{1}", InstanceData.MapId, InstanceData.Id);
            }

        }

        /// <summary>
        /// Adds an interactable to this map
        /// </summary>
        /// <param name="interactable"></param>
        public void AddInteractable(ServerInteractable interactable) 
        {
            lock (Interactables)
            {
                Interactables.Add(interactable.PublicData.Id, interactable);
                Program.InteractableSubscriptions.AddSubscriptionList(interactable.ToString());
            }
        }

        /// <summary>
        /// Removes an interactable from this map
        /// </summary>
        /// <param name="interactable"></param>
        public void RemoveInteractable(ServerInteractable interactable)
        {
            lock (Interactables)
            {
                Interactables.Remove(interactable.PublicData.Id);
                Program.InteractableSubscriptions.RemoveSubscriptionList(interactable.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public void HandleSubscriptionPush(Message msg)
        {
            Console.WriteLine("Got subscription push");
        }
        //public StartInteractable(Data.Interactable interactable)
    }
}
