using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using ERAUtils.Logger;
using ProjectERA.Protocols;
using MongoDB.Bson;
using ERAServer.Services.Listeners;

namespace ERAServer.Protocols.Server.Misc
{
    internal partial class Misc : Protocol
    {
        /// <summary>
        /// Receives messages from origin of transfer
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="action"></param>
        private void UserTransfer(NetIncomingMessage msg)
        {
            // If initiator this is new, else this is ack
            Boolean fromInitiator = msg.ReadBoolean();

            if (fromInitiator)
            {
                // Create new transfer
                UserTransferData data = new UserTransferData()
                {
                    UserId = new ObjectId(msg.ReadBytes(12)),
                    ActiveId = new ObjectId(msg.ReadBytes(12)),
                    TransferId = new ObjectId(msg.ReadBytes(12)),
                    Username = msg.ReadString(),
                    SessionKey = msg.ReadBytes(32)
                };
                Clients.TransferedUsers.AddOrUpdate(data.UserId, data, (ObjectId key, UserTransferData value) => data);
                Logger.Debug("Received Transfer request from user id: " + data.UserId);

                // Ack packet
                NetOutgoingMessage omsg = OutgoingMessage(MiscAction.UserTransfer, 1);
                omsg.Write(false);
                omsg.Write(data.UserId.ToByteArray());
                omsg.Write(data.TransferId.ToByteArray());
                omsg.Write(Clients.Port);

                Connection.SendMessage(omsg, NetDeliveryMethod.ReliableUnordered);
            }
            else
            {
                UserTransferData value;

                // Send player transfer details
                if (Clients.PendingUserTransfers.TryRemove(new ObjectId(msg.ReadBytes(12)), out value))
                {
                    ObjectId transferId = new ObjectId(msg.ReadBytes(12));

                    // TODO: transfer id matching
                    Logger.Debug("Received Transfer acknowledgement (" + transferId + "/" + value.TransferId + ")");

                    // This will send the transfer data to the client
                    value.OnAckReceived.Invoke(msg.ReadInt32());
                }
                else
                {
                    // Error!
                    Logger.Error("Received transfer ack, but transfer not here!");
                }
            }
        }

        /// <summary>
        /// Sends message to destination of transfer
        /// </summary>
        /// <param name="data"></param>
        /// <remarks>Packet is 70+ bytes</remarks>
        public void TransferUser(UserTransferData data)
        {
            // Mark for ack
            Clients.PendingUserTransfers.AddOrUpdate(data.UserId, data, (ObjectId key, UserTransferData value) => data);

            // Send request
            Int32 bytes = Encoding.UTF8.GetByteCount(data.Username);
            NetOutgoingMessage msg = OutgoingMessage(MiscAction.UserTransfer, 1 + 12 + 12 + 12 + 32 + bytes + (bytes > 127 ? 2 : 1));
            msg.Write(true); 
            msg.Write(data.UserId.ToByteArray());
            msg.Write(data.ActiveId.ToByteArray());
            msg.Write(data.TransferId.ToByteArray());
            msg.Write(data.Username);
            msg.Write(data.SessionKey);
            this.Connection.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal struct UserTransferData
    {
        /// <summary>
        /// Transfer id is the id of the transfer
        /// </summary>
        public ObjectId TransferId { get; set; }

        /// <summary>
        /// User id is the id of the user being transfered
        /// </summary>
        public ObjectId UserId { get; set; }

        /// <summary>
        /// Username of the transfering user
        /// </summary>
        public String Username { get; set; }

        /// <summary>
        /// Sessionkey of the user being transfered
        /// </summary>
        public Byte[] SessionKey { get; set; }

        /// <summary>
        /// Will yield when ack is received.
        /// </summary>
        internal Action<Int32> OnAckReceived { get; set; }

        public ObjectId ActiveId { get; set; }
    }
}
