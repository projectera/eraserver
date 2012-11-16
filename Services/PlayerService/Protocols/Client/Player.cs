using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using MongoDB.Bson;
using System.Threading;
using ERA.Services.Player.Connections;
using ERA.Protocols.ClientProtocol;

namespace ERA.Services.Player.Protocols.Client
{
    internal partial class Player : Protocol
    {
        private ReaderWriterLockSlim _playerDataLock;
        private ERA.Protocols.PlayerProtocol.Player _playerData;

        /// <summary>
        /// Player
        /// </summary>
        internal ERA.Protocols.PlayerProtocol.Player PlayerData 
        {
            get
            {
                try
                {
                    _playerDataLock.EnterReadLock();
                    return _playerData; 
                }
                finally
                {
                    _playerDataLock.ExitReadLock();
                }

            }

            set
            {
                _playerDataLock.EnterWriteLock();
                _playerData = value;
                _playerDataLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Player Id
        /// </summary>
        internal ObjectId Id 
        { 
            get 
            { 
                return PlayerData.Id; 
            } 
        }

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
            get { return (Byte)ClientProtocols.Player; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection"></param>
        public Player(ClientConnection connection, String username)
            : base(connection)
        {
            _playerDataLock = new ReaderWriterLockSlim();
            this.PlayerData = Data.Player.GetBlocking(username) ?? new ERA.Protocols.PlayerProtocol.Player();
            connection.NodeId = this.PlayerData.Id;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection"></param>
        public Player(ClientConnection connection, ObjectId id)
            : base(connection)
        {
            _playerDataLock = new ReaderWriterLockSlim();
            _playerDataLock.EnterWriteLock();
            this.PlayerData = Data.Player.GetBlocking(id) ?? new ERA.Protocols.PlayerProtocol.Player();
            connection.NodeId = this.PlayerData.Id;
            _playerDataLock.ExitWriteLock();
        }
        
        /// <summary>
        /// Processes Incoming Message
        /// </summary>
        /// <param name="msg"></param>
        internal override void IncomingMessage(Lidgren.Network.NetIncomingMessage msg)
        {
            PlayerAction action = (PlayerAction)msg.ReadRangedInteger(0, (Int32)PlayerAction.Max);
            msg.SkipPadBits();

            switch (action)
            {
                //
                case PlayerAction.Get:
                    // Obtain player search Id
                    ObjectId searchPlayerId = new ObjectId(msg.ReadBytes(12));

                    // Action
                    this.QueueAction(() =>
                    {
                        // Find the player 
                        Data.Player.Get(searchPlayerId == ObjectId.Empty ? this.PlayerData.Id : searchPlayerId).ContinueWith(
                            (pt) => QueueAction(() =>
                            {
                                ERA.Protocols.PlayerProtocol.Player player = pt.Result;
                                ERA.Protocols.PlayerProtocol.Player tempPlayer = null;
                                if (player == null)
                                    tempPlayer = new ERA.Protocols.PlayerProtocol.Player();

                                // Log this action
                                //Logger.Verbose("PlayerAction.Get requested: " + searchPlayerId + " and got " + (player ?? tempPlayer).Id);

                                // Create the message and encode data
                                NetOutgoingMessage getMsg = OutgoingMessage(PlayerAction.Get);
                                
                                getMsg.Write(searchPlayerId.ToByteArray());
                                ERA.Protocols.PlayerProtocol.Player.Pack(player ?? tempPlayer, getMsg);

                                // Send the message
                                this.Connection.SendMessage(getMsg, NetDeliveryMethod.ReliableUnordered);

                                // Recycle if needed
                                if (tempPlayer != null)
                                    tempPlayer.Clear();
                            })
                        );
                    });
                    break;

                case PlayerAction.GetAvatars:

                    break;

                //
                case PlayerAction.RequestMovement:
                   
                    break;

                // Sends a message
                case PlayerAction.Message:
                    
                    break;

                case PlayerAction.MessageStatus:
                   
                    break;

                case PlayerAction.MessageStart:
                   
                    break;

                case PlayerAction.MessageParticipant:
                    
                    break;

                //
                case PlayerAction.PickAvatar:
                    // Obtain avatar picked id
                    ObjectId searchPickId = new ObjectId(msg.ReadBytes(12));

                    // Log this action
                    //Logger.Verbose("PlayerAction.PickAvatar requested: " + searchPickId);

                    // Action
                    QueueAction(() =>
                    {
                        
                        if (this.PlayerData.Interactables.Any(a => a.Equals(searchPickId)))
                        {
                            // Set the interactable
                            _playerDataLock.EnterWriteLock();
                            this.PlayerData.ActiveInteractable = searchPickId;
                            _playerDataLock.ExitWriteLock();
                        }
                        else
                        {
                            // Can not pick that character
                            this.Connection.NetConnection.Disconnect("Illegal action.");

                            // Throw an error
                            throw new ArgumentException("Avatar Id to pick is not known in players avatar list.");
                        }
                    });
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
        private NetOutgoingMessage OutgoingMessage(PlayerAction action)
        {
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier);
            msg.WriteRangedInteger(0, (Int32)PlayerAction.Max, (Int32)action);
            msg.WritePadBits();
            return msg;
        }

        /// <summary>
        /// Creates an Outgoing Message with the specified action integer written to it.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private NetOutgoingMessage OutgoingMessage(PlayerAction action, Int32 initialCapacity)
        {
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier, initialCapacity + 1);
            msg.WriteRangedInteger(0, (Int32)PlayerAction.Max, (Int32)action);
            msg.WritePadBits();
            return msg;
        }
        
        /// <summary>
        /// This functions runs when the client disconnects,
        /// </summary>
        /// <remarks>Before Deregister</remarks>
        internal override void Disconnect()
        {
            base.Disconnect();
        }

        /// <summary>
        /// This functions runs when the client is disconnected
        /// </summary>
        /// <remarks>After Deregister</remarks>
        public override void Dispose()
        {
            base.Dispose();

            // Queue reference clearing, because some action may already got it
            // from cache and is using it. Not sure if that can happen though (DISCUSS)
            QueueAction(() =>
                {
                    _playerDataLock.EnterWriteLock();
                    _playerData = null;
                    _playerDataLock.ExitWriteLock();
                });
        }
    }
}
