using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using ProjectERA.Protocols;
using MongoDB.Bson;
using System.Threading;
using ERAUtils;
using ERAUtils.Logger;

namespace ERAServer.Protocols.Client
{
    internal partial class Interactable : Protocol
    {
        /// <summary>
        /// Private static list of this protocols instances
        /// </summary>
        private static List<Protocol> _instances;

        /// <summary>
        /// Player Instances
        /// </summary>
        /// <remarks>Static</remarks>
        public override List<Protocol> Instances
        {
            get
            {
                return _instances;
            }
            set
            {
                _instances = value;
            }
        }

        /// <summary>
        /// The id of this protocol
        /// </summary>
        public override Byte ProtocolIdentifier
        {
            get { return (Byte)ClientProtocols.Interactable; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection"></param>
        public Interactable(Connection connection)
            : base(connection)
        {
            Data.GeneralCache<ObjectId, Data.Interactable>.InitializeCache();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        internal override void IncomingMessage(Lidgren.Network.NetIncomingMessage msg)
        {
            InteractableAction action = (InteractableAction)msg.ReadRangedInteger(0, (Int32)InteractableAction.Max);
            msg.SkipPadBits();
            switch (action)
            {
                case InteractableAction.Get:
                    // Obtain search id
                    ObjectId search = new ObjectId(msg.ReadBytes(12));
                    
                    // Action
                    this.QueueAction(() =>
                    {
                        // Find the interactacble 
                        Data.Interactable interactable = Interactable.Find(search);

                        // Get temp if nothing fetched
                        Data.Interactable tempInteractable = null;
                        if (interactable == null)
                            tempInteractable = new Data.Interactable();

                        // Log this action
                        Logger.Verbose("InteractableAction.Get requested: " + search + " and got " + (interactable ?? tempInteractable).Id);

                        // Create the message and encode data
                        NetOutgoingMessage getMsg = OutgoingMessage(InteractableAction.Get);
                        getMsg.Write(search.ToByteArray());
                        Interactable.Pack(interactable ?? tempInteractable, ref getMsg);

                        // Send the message
                        this.Connection.SendMessage(getMsg, NetDeliveryMethod.ReliableUnordered);

                        // Recycle temp if needed
                        if (tempInteractable != null)
                            tempInteractable.Clear();
                    });
                    break;

                // Broadcast message
                case InteractableAction.Message:
                    String message = msg.ReadString();
                    Protocol protocolMessage;
                    this.Connection.TryGetProtocol(typeof(Player), out protocolMessage);
                    ObjectId messageId = (protocolMessage as Player).InteractableData.Id;
                    ObjectId messageMapId = (protocolMessage as Player).InteractableData.MapId;
                    lock (this.Instances)
                        this.Instances.ForEach((a) => BroadcastMessage(messageId, messageMapId, message, a)); 
                    break;

                default:
                    throw new NetException("No such action in protocol.");
            }
        }

        /// <summary>
        /// Creates an Outgoing Message with the specified action integer written to it.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        internal NetOutgoingMessage OutgoingMessage(InteractableAction action)
        {
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier);
            msg.WriteRangedInteger(0, (Int32)InteractableAction.Max, (Int32)action);
            msg.WritePadBits();
            return msg;
        }

        /// <summary>
        /// Creates an Outgoing Message with the specified action integer written to it.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        internal NetOutgoingMessage OutgoingMessage(InteractableAction action, Int32 initialCapacity)
        {
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier, initialCapacity + ERAUtils.BitManipulation.BytesToHold((Int32)InteractableAction.Max));
            msg.WriteRangedInteger(0, (Int32)InteractableAction.Max, (Int32)action);
            msg.WritePadBits();
            return msg;
        }
    }
}
