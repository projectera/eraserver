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
using ERAServer.Services;

namespace ERAServer.Protocols.Client
{
    internal partial class Player : Protocol
    {
        private ReaderWriterLockSlim _playerDataLock;
        private Data.Player _playerData;

        private ReaderWriterLockSlim _interactableDataLock;
        private Data.Interactable _interactableData;

        /// <summary>
        /// Player
        /// </summary>
        internal Data.Player PlayerData 
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

        /// <summary>
        /// Interactable/Active Avatar
        /// </summary>
        internal Data.Interactable InteractableData
        {
            get
            {
                try
                {
                    _interactableDataLock.EnterReadLock();
                    return _interactableData;
                }
                finally
                {
                    _interactableDataLock.ExitReadLock();
                }

            }

            set
            {
                _interactableDataLock.EnterWriteLock();
                _interactableData = value;
                _interactableDataLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Interactable/Active Avatar Id
        /// </summary>
        internal ObjectId ActiveId
        {
            get
            {
                try
                {
                    _interactableDataLock.EnterReadLock();

                    if (_interactableData != null)
                        return _interactableData.Id;

                    return ObjectId.Empty;
                }
                finally
                {
                    _interactableDataLock.ExitReadLock();
                }

            }
            set
            {
                _interactableDataLock.EnterWriteLock();
                _interactableData = Data.Interactable.GetBlocking(value);
                _interactableDataLock.ExitWriteLock();

                // Add to dictionairy
                Data.GeneralCache<ObjectId, Data.Interactable>.UpdateCache(this.InteractableData.Id, this.InteractableData);
            }
        }

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
            get { return (Byte)ClientProtocols.Player; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection"></param>
        public Player(Connection connection, String username)
            : base(connection)
        {
            Data.GeneralCache<ObjectId, Data.Player>.InitializeCache();
            Data.GeneralCache<ObjectId, Data.Interactable>.InitializeCache();

            _playerDataLock = new ReaderWriterLockSlim();
            _interactableDataLock = new ReaderWriterLockSlim();
            this.PlayerData = Data.Player.GetBlocking(username) ?? new Data.Player();
            connection.NodeId = this.PlayerData.Id;

            // We don't want the playerdata still being null when it is added
            Thread.MemoryBarrier();

            // Add to dictionairy
            Data.GeneralCache<ObjectId, Data.Player>.AddCache(this.PlayerData.Id, this.PlayerData);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection"></param>
        public Player(Connection connection, ObjectId id)
            : base(connection)
        {
            Data.GeneralCache<ObjectId, Data.Player>.InitializeCache();
            Data.GeneralCache<ObjectId, Data.Interactable>.InitializeCache();

            _playerDataLock = new ReaderWriterLockSlim();
            _interactableDataLock = new ReaderWriterLockSlim();
            this.PlayerData = Data.Player.GetBlocking(id) ?? new Data.Player();
            connection.NodeId = this.PlayerData.Id;

            // We don't want the playerdata still being null when it is added
            Thread.MemoryBarrier();

            // Add to dictionairy
            Data.GeneralCache<ObjectId, Data.Player>.AddCache(this.PlayerData.Id, this.PlayerData);
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
                        Data.Player player = Player.Find(searchPlayerId == ObjectId.Empty ? this.PlayerData.Id : searchPlayerId);

                        Data.Player tempPlayer = null;
                        if (player == null)
                            tempPlayer = new Data.Player();

                        // Log this action
                        Logger.Verbose("PlayerAction.Get requested: " + searchPlayerId + " and got " + (player ?? tempPlayer).Id);

                        // Create the message and encode data
                        NetOutgoingMessage getMsg = OutgoingMessage(PlayerAction.Get);
                        getMsg.Write(searchPlayerId.ToByteArray());
                        Player.Pack(player ?? tempPlayer, ref getMsg);

                        // Send the message
                        this.Connection.SendMessage(getMsg, NetDeliveryMethod.ReliableUnordered);

                        // Recycle if needed
                        if (tempPlayer != null)
                            tempPlayer.Clear();
                    });
                    break;

                //
                case PlayerAction.RequestMovement:
                    if (InteractableData != null)
                    {
                        Int32 newX = msg.ReadInt32();
                        Int32 newY = msg.ReadInt32();
                        Byte newD = msg.ReadByte();

                        // TODO: check position off course

                        Data.AI.InteractableAppearance component = InteractableData.GetComponent(typeof(Data.AI.InteractableAppearance)) as Data.AI.InteractableAppearance;
                        InteractableData.MapX = newX;
                        InteractableData.MapY = newY;
                        component.MapDir = newD;

                        lock (this.Instances)
                            this.Instances.ForEach((protocol) => {
                                if (protocol != this)
                                {
                                    Player pProtocol = (Player)protocol;
                                    NetOutgoingMessage broadcastMoveMsg = pProtocol.OutgoingInteractableMessage(InteractableAction.Movement, 21);
                                    if (pProtocol.InteractableData != null)
                                    {
                                        broadcastMoveMsg.Write(this.InteractableData.Id.ToByteArray());
                                        broadcastMoveMsg.Write(newX);
                                        broadcastMoveMsg.Write(newY);
                                        broadcastMoveMsg.Write(newD);

                                        protocol.Connection.SendMessage(broadcastMoveMsg, NetDeliveryMethod.UnreliableSequenced, 11); // NetDeliveryMethod.ReliableSequenced, 11); // Find a way to use RealiableSequenced
                                    }
                                }
                            });
                    }
                    break;

                // Sends a message
                case PlayerAction.Message:
                    ObjectId messageReceiver = new ObjectId(msg.ReadBytes(12));
                    String messageContents = msg.ReadString();

                    /*Data.Dialogue.Get(messageReceiver).ContinueWith(
                        (task) =>
                        {*/
                            //task.Result.AddMessage(message);

                            // Add message to dialogue and save it
                            Data.DialogueMessage message = Data.DialogueMessage.Generate(this.PlayerData.Id, messageContents);
                            this.PlayerData.Dialogues[messageReceiver].AddMessage(message);

                            // Broadcast message to online players
                            List<NetConnection> receipients = new List<NetConnection>();
                            lock (this.Instances)
                            {
                                this.Instances.ForEach((protocol) =>
                                {
                                    ObjectId receipientId = (protocol as Player).PlayerData.Id;

                                    if (this.PlayerData.Dialogues[messageReceiver].Participants.Contains(receipientId))
                                    {
                                        receipients.Add(protocol.Connection.NetConnection);
                                    }
                                });
                            }

                            if (receipients.Count > 0)
                            {
                                Int32 messageCount = Encoding.UTF8.GetByteCount(messageContents);
                                NetOutgoingMessage messageBroadcast = OutgoingMessage(PlayerAction.Message, 45 + messageCount);
                                messageBroadcast = message.Pack(ref messageBroadcast);
                                this.Connection.NetManager.SendMessage(messageBroadcast, receipients, NetDeliveryMethod.ReliableUnordered, 0);
                            }
                        //});
                    break;

                case PlayerAction.MessageAttachment:
                    // TODO add attachment to message
                    break;

                case PlayerAction.MessageStatus:
                    ObjectId dialogueStatusId = new ObjectId(msg.ReadBytes(12));
                    ObjectId dialogueMessageId = new ObjectId(msg.ReadBytes(12));

                    if (msg.ReadBoolean())
                    {
                        this.PlayerData.Dialogues[dialogueStatusId].MarkAsRead(dialogueMessageId, PlayerData.Id);
                    }
                    else
                    {
                        this.PlayerData.Dialogues[dialogueStatusId].MarkAsUnread(dialogueMessageId, PlayerData.Id);
                    }
                    break;

                case PlayerAction.MessageStart:
                    ObjectId messageStartReceiver = new ObjectId(msg.ReadBytes(12));
                    String messageStartContents = msg.ReadString();
                    Data.Dialogue messageStart = Data.Dialogue.Generate(this.PlayerData.Id, messageStartReceiver, messageStartContents);
                    this.PlayerData.Dialogues.Add(messageStart.Id, messageStart);
                    messageStart.Put();

                    Protocol messageStartProtocol = null;
                    lock (this.Instances)
                    {
                        messageStartProtocol = this.Instances.Find((a) => (a as Player).PlayerData.Id == messageStartReceiver);
                    }

                    if (messageStartProtocol != null)
                    {
                        Int32 messageStartCount = Encoding.UTF8.GetByteCount(messageStartContents);
                        NetOutgoingMessage messageStartBroadcast = OutgoingMessage(PlayerAction.MessageStart, 45 + messageStartCount);
                        messageStartBroadcast = messageStart.Pack(ref messageStartBroadcast);
                        messageStartProtocol.Connection.SendMessage(messageStartBroadcast, NetDeliveryMethod.ReliableUnordered);
                    }
                    break;

                case PlayerAction.MessageParticipant:
                    ObjectId messageParticipantDialogue  = new ObjectId(msg.ReadBytes(12));
                    ObjectId messageParticipantId = new ObjectId(msg.ReadBytes(12));

                    this.PlayerData.Dialogues[messageParticipantDialogue].AddParticipant(messageParticipantId);

                    Protocol messageParticipantProtocol = null;
                    lock (this.Instances)
                    {
                        messageParticipantProtocol = this.Instances.Find((a) => (a as Player).PlayerData.Id == messageParticipantDialogue);
                    }

                    if (messageParticipantProtocol != null)
                    {
                        NetOutgoingMessage messageParticipantBroadcast = OutgoingMessage(PlayerAction.MessageParticipant);
                        messageParticipantBroadcast = this.PlayerData.Dialogues[messageParticipantDialogue].Pack(ref messageParticipantBroadcast);
                        messageParticipantProtocol.Connection.SendMessage(messageParticipantBroadcast, NetDeliveryMethod.ReliableUnordered);
                    }
                    break;

                //
                case PlayerAction.PickAvatar:
                    // Obtain avatar picked id
                    ObjectId searchPickId = new ObjectId(msg.ReadBytes(12));

                    // Log this action
                    Logger.Verbose("PlayerAction.PickAvatar requested: " + searchPickId);

                    // Action
                    QueueAction(() =>
                    {
                        if (this.PlayerData.AvatarIds.Any(a => a.Equals(searchPickId)))
                        {
                            // Set the interactable
                            this.ActiveId = searchPickId;
                            
                            // Start moving to the correct server : Transfer User
                            if (!TransferUser(this.InteractableData.MapId))
                            {
                                // We need to have the map here
                                Logger.Debug("Pickavatar result: Map local");

                                // HACK: Always run map
                                MapManager.StartRunningMap(this.InteractableData.MapId);

                                // Issue map enter
                                EnterMap();

                                // Send the map
                                this.Connection.SendMessage(PickAvatarResponse(), NetDeliveryMethod.ReliableUnordered);
                            }
                            else
                            {
                                // The map is somewhere else, transfer was executed?
                                Logger.Debug("Pickavatar result: Map remote");
                            }
                            
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
        /// 
        /// </summary>
        /// <param name="interactableAction"></param>
        /// <param name="initialCapacity"></param>
        /// <returns></returns>
        private NetOutgoingMessage OutgoingInteractableMessage(InteractableAction interactableAction, Int32 initialCapacity)
        {
            Protocol protocol;
            this.Connection.TryGetProtocol(typeof(Interactable), out protocol);
            NetOutgoingMessage msg = Connection.MakeMessage(protocol.ProtocolIdentifier, initialCapacity + ERAUtils.BitManipulation.BytesToHold((Int32)InteractableAction.Max));
            msg.WriteRangedInteger(0, (Int32)InteractableAction.Max, (Int32)interactableAction);
            msg.WritePadBits();
            return msg;
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
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier, initialCapacity + ERAUtils.BitManipulation.BytesToHold((Int32)PlayerAction.Max));
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
            // Save Changes
            if (InteractableData != null)
            {
                InteractableData.Put();
                LeaveMap();
            }

            base.Disconnect();
        }

        /// <summary>
        /// This functions runs when the client is disconnected
        /// </summary>
        /// <remarks>After Deregister</remarks>
        public override void Dispose()
        {
            base.Dispose();

            Data.GeneralCache<ObjectId, Data.Player>.RemoveCache(this.Id);

            try
            {
                if (_playerData != null)
                    foreach (ObjectId iid in _playerData.AvatarIds)
                        Data.GeneralCache<ObjectId, Data.Interactable>.RemoveCache(iid);
            }
            catch (NullReferenceException)
            {

            }

            // Queue reference clearing, because some action may already got it
            // from cache and is using it. Not sure if that can happen though (DISCUSS)
            QueueAction(() =>
                {
                    _playerDataLock.EnterWriteLock();
                    _playerData = null;
                    _playerDataLock.ExitWriteLock();

                    _interactableDataLock.EnterWriteLock();
                    _interactableData = null;
                    _interactableDataLock.ExitWriteLock();
                });
        }
    }
}
