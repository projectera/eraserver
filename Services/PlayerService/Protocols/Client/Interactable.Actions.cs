using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using ProjectERA.Protocols;
using Lidgren.Network;

namespace ERAServer.Protocols.Client
{
    internal partial class Interactable : Protocol
    {
        /// <summary>
        /// Finds a Interactable by id
        /// </summary>
        /// <param name="search">id of Interactable</param>
        /// <returns>Data.Interactable</returns>
        internal static Data.Interactable Find(ObjectId search)
        {
            // Case 1: in cache
            Data.Interactable data = Data.GeneralCache<ObjectId, Data.Interactable>.QueryCache(search);
            if (data != null)
                return data;

            // Default: Looking for other interactable, fetch
            return Data.Interactable.GetBlocking(search);
        }

        /// <summary>
        /// Encoded a interactable object from to an outgoing message
        /// </summary>
        /// <remarks>Needs to be synchronized with Decode de on Receiver</remarks>
        /// <param name="interactable">Interactable to encode</param>
        /// <param name="msg">msg to encode to</param>
        /// <returns>msg with encoded interactable</returns>
        internal static NetOutgoingMessage Pack(Data.Interactable interactable, ref NetOutgoingMessage msg)
        {
            // result.UserId = ReadBytes 4 + 3 + 2 + 3 

            msg.Write(interactable.Id.ToByteArray());
            msg.Write(interactable.Name);
            msg.Write((Byte)interactable.StateFlags);
            msg.Write(interactable.MapId.ToByteArray());
            msg.Write(interactable.MapX);
            msg.Write(interactable.MapY);

            lock (interactable.Components)
            {
                msg.Write(interactable.Components.Count);

                foreach (Data.AI.InteractableComponent component in interactable.Components.Values)
                    component.Pack(ref msg);
            }

            return msg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="interactable"></param>
        private void BroadcastMessage(ObjectId id, ObjectId mapId, String message, Protocol destination)
        {
            if (destination != null)
            {
                Protocol map;
                destination.Connection.TryGetProtocol(typeof(Map), out map);
                if (((Map)map).CurrentMapId.Equals(mapId))
                {
                    NetOutgoingMessage msg = OutgoingMessage(InteractableAction.Message);
                    msg.Write(id.ToByteArray());
                    msg.Write(message);

                    destination.QueueAction(() =>
                    {
                        destination.Connection.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
                    });
                }
            }
        }
    }
}
