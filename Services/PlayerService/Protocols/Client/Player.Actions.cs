using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using System.Threading.Tasks;
using System.Threading;
using ERAServer.Protocols.Server.Misc;
using Lidgren.Network;
using System.Net;
using ProjectERA.Protocols;
using ERAUtils.Logger;
using ERAServer.Services;
using ERAServer.Services.Listeners;

namespace ERAServer.Protocols.Client
{
    internal partial class Player : Protocol
    {
        /// <summary>
        /// Finds a player by id
        /// </summary>
        /// <param name="search">id of player</param>
        /// <returns>Data.Player</returns>
        internal static Data.Player Find(ObjectId search)
        {        
            // Case 1: Looking for logged-in player
            Data.Player data = Data.GeneralCache<ObjectId, Data.Player>.QueryCache(search);

            if (data != null)
                return data;

            // Default: Looking for logged-out player, fetch
            return Data.Player.GetBlocking(search);
        }

        /// <summary>
        /// Encoded a player object from to an outgoing message
        /// </summary>
        /// <remarks>Needs to be synchronized with Decode de on Receiver</remarks>
        /// <param name="player">Player to encode</param>
        /// <param name="msg">msg to encode to</param>
        /// <returns>msg with encoded player</returns>
        internal static NetOutgoingMessage Pack(Data.Player player, ref NetOutgoingMessage msg)
        {
            msg.Write(player.Id.ToByteArray());
            msg.Write(player.ForumId);
            msg.Write(player.Username);
            msg.Write(player.EmailAddress);

            if (player.AvatarIds != null)
            {
                msg.Write(player.AvatarIds.Count);
                IOrderedEnumerable<ObjectId> result = player.AvatarIds.OrderBy(a => a.CreationTime);
                foreach (ObjectId id in result)
                {
                    msg.Write(id.ToByteArray());
                }
            }
            else
            {
                msg.Write((Int32)0);
            }

            return msg;
        }

        /// <summary>
        /// Gets PickAvatar Response message
        /// </summary>
        /// <returns></returns>
        internal NetOutgoingMessage PickAvatarResponse()
        {
            NetOutgoingMessage pamsg = OutgoingMessage(PlayerAction.PickAvatar);
            Interactable.Pack(Interactable.Find(this.ActiveId), ref pamsg);
            return pamsg;
        }

        /// <summary>
        /// Enters the map
        /// </summary>
        internal void EnterMap()
        {
            Protocol mprotocol;
            if (this.Connection.TryGetProtocol((Byte)ClientProtocols.Map, out mprotocol))
            {
                // Issue map join
                ObjectId mapId = this.InteractableData.MapId;
                MapManager.InteractableJoined(mapId, this.InteractableData);
                (mprotocol as Map).CurrentMapId = mapId;

                lock (this.Instances)
                    this.Instances.ForEach((p) => BroadcastInteractableJoined(mapId, this.InteractableData, p));
            }
            else
            {
                throw new Exception("Map protocol does not exist.");
            }

        }

        /// <summary>
        /// 
        /// </summary>
        internal void LeaveMap()
        {
            Protocol mprotocol;
            if (this.Connection.TryGetProtocol((Byte)ClientProtocols.Map, out mprotocol))
            {
                // Issue map unjoin
                ObjectId mapId = this.InteractableData.MapId;
                MapManager.InteractableLeft(this.InteractableData);

                // Exit map
                ((Map)mprotocol).ExitedMap();

                // Broadcast
                lock (this.Instances)
                    this.Instances.ForEach((p) => BroadcastInteractableLeft(mapId, this.InteractableData, p));
            }
            else
            {
                throw new Exception("Map protocol does not exist.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="interactable"></param>
        /// <param name="destination"></param>
        private void BroadcastInteractableJoined(ObjectId mapId, Data.Interactable interactable, Protocol destination)
        {
            if (destination != null)
            {
                Protocol map;
                destination.Connection.TryGetProtocol(typeof(Map), out map);
                if (((Map)map).CurrentMapId.Equals(mapId))
                {
                    ((Map)map).InteractableJoined(mapId, interactable);

                    Logger.Debug("Broadcast of joined interactable [" + interactable.Id.ToString() + "] to " + destination.Connection.NetConnection.RemoteEndpoint);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="interactable"></param>
        private void BroadcastInteractableLeft(ObjectId mapId, Data.Interactable interactable, Protocol destination)
        {
            if (destination != null)
            {
                Protocol map;
                destination.Connection.TryGetProtocol(typeof(Map), out map);
                if (((Map)map).CurrentMapId.Equals(mapId))
                {
                    ((Map)map).InteractableLeft(mapId, interactable);

                    Logger.Debug("Broadcast of left interactable [" + interactable.Id.ToString() + "] to " + destination.Connection.NetConnection.RemoteEndpoint);
                
                }
            }
        }

        /// <summary>
        /// Transfers user to server that holds mapId
        /// <param name="mapId"></param>
        /// </summary>
        internal Boolean TransferUser(ObjectId mapId)
        {
            // Gets destination
            Connection destination;
            if (!Servers.MapConnectionMapping.TryGetValue(mapId, out destination))
                return false; // Map not yet on any server, so start it here?

            // Create utd
            UserTransferData utd = new UserTransferData()
            {
                UserId = this.Id,
                ActiveId = this.ActiveId,
                Username = this.PlayerData.Username,
                TransferId = ObjectId.GenerateNewId(),
                //SessionKey = this.Connection.NetConnection.GetSessionBytes(), <<< THIS LINE REWRITE TODO

                // On ack, tell user to start transfering
                OnAckReceived = (port) =>
                {
                    // Send transfer message
                    NetOutgoingMessage msg = this.Connection.MakeMessage((Byte)ProjectERA.Protocols.ClientProtocols.Player, 5);
                    msg.WriteRangedInteger(0, (Int32)ProjectERA.Protocols.PlayerAction.Max, (Int32)ProjectERA.Protocols.PlayerAction.Transfer);
                    msg.WritePadBits();
                    msg.Write(new IPEndPoint(destination.NetConnection.RemoteEndpoint.Address, port)); // destination address for transfer
                    this.Connection.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
                }
            };

            // Invoke correct misc protocol  
            Protocol protocol;
            if (destination.TryGetProtocol((Byte)Protocols.Server.ServerProtocols.Misc, out protocol))
            {
                // Start user transfer
                ((Misc)protocol).TransferUser(utd);
                return true;
            }

            // Server does not want user transfers
            return false;
        }
    }
}
