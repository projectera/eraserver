using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectERA.Protocols;
using Lidgren.Network;
using MongoDB.Bson;
using ERAUtils.Logger;
using ERAServer.Services;

namespace ERAServer.Protocols.Client
{
    internal partial class Map : Protocol
    {
        private Boolean _initialized;
        private List<NetOutgoingMessage> _preInitializationMessage;

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
            get { return (Byte)ClientProtocols.Map; }
        }

        /// <summary>
        /// 
        /// </summary>
        public ObjectId CurrentMapId
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection">Connection to bind to</param>
        /// <param name="networkManager"></param>
        public Map(Connection connection)
            : base(connection)
        {
            this.CurrentMapId = ObjectId.Empty;

            _initialized = false;
            _preInitializationMessage = new List<NetOutgoingMessage>();
        }

        /// <summary>
        /// Processes Incoming Message
        /// </summary>
        /// <param name="msg"></param>
        internal override void IncomingMessage(Lidgren.Network.NetIncomingMessage msg)
        {
            MapAction action = (MapAction)msg.ReadRangedInteger(0, (Int32)MapAction.Max);
            msg.SkipPadBits();

            switch (action)
            {
                // Gets a map
                case MapAction.Get:
                    ObjectId gid = new ObjectId(msg.ReadBytes(12));
                    if (gid.Equals(ObjectId.Empty))
                    {
                        Protocol protocol;
                        if (this.Connection.TryGetProtocol(typeof(Player), out protocol))
                        {
                            gid = ((Player)protocol).InteractableData.MapId;
                        } 
                        else 
                        {
                            throw new InvalidOperationException("Could not retrieve current map id of player");
                        }
                    }

                    NetOutgoingMessage getMsg = OutgoingMessage(MapAction.Get);
                    MapManager.WriteMapData(gid, getMsg);

                    this.Connection.SendMessage(getMsg, NetDeliveryMethod.ReliableOrdered);
                    break;

                // Gets the tileset data
                case MapAction.GetTilesetData:
                    ObjectId gtid = new ObjectId(msg.ReadBytes(12));
                    if (gtid.Equals(ObjectId.Empty))
                    {
                        Protocol protocol;
                        if (this.Connection.TryGetProtocol(typeof(Player), out protocol))
                        {
                            gtid = ((Player)protocol).InteractableData.MapId;
                            gtid = MapManager.GetTilesetId(gtid);
                        }
                        else
                        {
                            throw new InvalidOperationException("Could not retrieve current map id of player");
                        }
                    }

                    NetOutgoingMessage getTilesetMsg = OutgoingMessage(MapAction.GetTilesetData);
                    MapManager.WriteTilesetData(gtid, getTilesetMsg);

                    this.Connection.SendMessage(getTilesetMsg, NetDeliveryMethod.ReliableOrdered);
                    break;

                // Gets interactables on a map
                case MapAction.GetInteractables:
                    Queue<ObjectId> ids = new Queue<ObjectId>(MapManager.GetInteractables(this.CurrentMapId));
                    
                    Int32 part = 0;
                    Int32 parts = (Int32)Math.Ceiling(ids.Count / 100f);

                    while (ids.Count > 0)
                    {
                        // NOTE: only 116 fit in one mtu, splitting it myself is faster and
                        // better then using fragmented messages in Lidgren
                        Int32 length = Math.Min(100, ids.Count);
                        
                        NetOutgoingMessage getInteractablesMsg = OutgoingMessage(MapAction.GetInteractables, length * 12 + 12);
                        getInteractablesMsg.Write(part++);
                        getInteractablesMsg.Write(parts);
                        getInteractablesMsg.Write(length);
                        while(length-- > 0)
                        {
                            getInteractablesMsg.Write(ids.Dequeue().ToByteArray());
                        }

                        this.Connection.SendMessage(getInteractablesMsg, NetDeliveryMethod.ReliableUnordered);
                    }

                    this.QueueAction(() => 
                    {
                        _initialized = true;

                        Logger.Info(new String[] { "Sending ", _preInitializationMessage.Count.ToString(), " pre init messages"});

                        foreach (NetOutgoingMessage unsentMsg in _preInitializationMessage)
                        {
                            this.Connection.SendMessage(unsentMsg, NetDeliveryMethod.ReliableUnordered);
                        }

                        _preInitializationMessage.Clear();
                    });
                    
                    break;

                // Validates the data
                case MapAction.ValidateData:
                    ObjectId validateDataOneTimeKey = new ObjectId(msg.ReadBytes(12));

                    // Check map
                    ObjectId validateDataMapId = new ObjectId(msg.ReadBytes(12));
                    UInt32 validateDataMapVersion = msg.ReadUInt32();
                    Int32 validateDataMapHash = msg.ReadInt32(); // TODO: hash check
                    Boolean upToDateMap = validateDataMapId != ObjectId.Empty &&
                        MapManager.GetMapVersion(validateDataMapId) == validateDataMapVersion &&
                        MapManager.GetMapHashCode(validateDataMapId) == validateDataMapHash;

                    // Check tileset
                    ObjectId validateDataTilesetId = new ObjectId(msg.ReadBytes(12));
                    UInt32 validateDataTilesetVersion = msg.ReadUInt32();
                    Int32 validateDataTilesetHash = msg.ReadInt32();
                    Boolean upToDateTileset = validateDataTilesetId != ObjectId.Empty &&
                        MapManager.GetTilesetVersion(validateDataTilesetId) == validateDataTilesetVersion &&
                        MapManager.GetTilesetHashCode(validateDataTilesetId) == validateDataTilesetHash;

                    // Log validation result
                    Logger.Debug("Validation [ " + validateDataOneTimeKey + " ] map result act/recv [ h:" +
                        MapManager.GetMapHashCode(validateDataMapId) + "/" + validateDataMapHash + " & v: " +
                        MapManager.GetMapVersion(validateDataMapId) + "/" + validateDataMapVersion + "] and " +
                        "tileset result [ h: " +
                        MapManager.GetTilesetHashCode(validateDataTilesetId) + "/" + validateDataTilesetHash + " & v: " +
                        MapManager.GetTilesetVersion(validateDataTilesetId) + "/" + validateDataTilesetVersion + "]");

                    // Return message
                    NetOutgoingMessage validateDataMessage = OutgoingMessage(MapAction.ValidateData, 13);
                    validateDataMessage.Write(validateDataOneTimeKey.ToByteArray());
                    validateDataMessage.Write((Int32)((upToDateMap ? (1 << 0) : 0) | (upToDateTileset ? (1 << 1) : 0)));

                    this.Connection.SendMessage(validateDataMessage, NetDeliveryMethod.ReliableUnordered);

                    break;
                
                // Validate graphics
                case MapAction.ValidateGraphics:

                    // Get validation key
                    ObjectId validateGraphicsOneTimeKey = new ObjectId(msg.ReadBytes(12));
                    Int32 validationResultField = 0;

                    // Get tileset
                    ObjectId validateTilesetId = new ObjectId(msg.ReadBytes(12));
                    Data.Tileset validateTilesetData = MapManager.GetTilesetData(validateTilesetId);
                    String validateTilesetMD5 = msg.ReadString();

                    Data.Asset validateTilesetAsset;
                    if (Data.Asset.GetFile(ERAUtils.Enum.AssetType.Tileset, validateTilesetData.AssetName, out validateTilesetAsset) == ERAUtils.Enum.AssetOperationResult.Ok)
                    {
                        validationResultField |= (validateTilesetMD5 == validateTilesetAsset.ServerMD5 ? (1 << 0) : 0);
                    }
                    else
                    {
                        validationResultField |= (1 << 0);
                    }

                    Logger.Debug("Validation [ " + validateGraphicsOneTimeKey + " ] graphics result act/recv [ h:" +
                        validateTilesetMD5 + "/" + validateTilesetAsset.ServerMD5 + "]");

                    // Get autotiles
                    for (Int32 i = 0; i < validateTilesetData.AutotileAssetNames.Count; i++)
                    {
                        validateTilesetMD5 = msg.ReadString();
                        if (Data.Asset.GetFile(ERAUtils.Enum.AssetType.Autotile, validateTilesetData.AutotileAssetNames[i], out validateTilesetAsset) == ERAUtils.Enum.AssetOperationResult.Ok)
                        {
                            validationResultField |= (validateTilesetMD5 == validateTilesetAsset.ServerMD5 ? (1 << (i + 1)) : 0);
                        }
                        else
                        {
                            validationResultField |= 1 << (i + 1);
                        }
                    }

                    // Return message
                    NetOutgoingMessage validateGraphicsMessage = OutgoingMessage(MapAction.ValidateGraphics, 16);
                    validateGraphicsMessage.Write(validateGraphicsOneTimeKey.ToByteArray());
                    validateGraphicsMessage.Write(validationResultField);

                    this.Connection.SendMessage(validateGraphicsMessage, NetDeliveryMethod.ReliableUnordered);
                    break;
            }
        }

        /// <summary>
        /// Creates an Outgoing Message with the specified action integer written to it.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        internal NetOutgoingMessage OutgoingMessage(MapAction action)
        {
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier);
            msg.WriteRangedInteger(0, (Int32)MapAction.Max, (Int32)action);
            msg.WritePadBits();
            return msg;
        }

        /// <summary>
        /// Creates an Outgoing Message with the specified action integer written to it.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private NetOutgoingMessage OutgoingMessage(MapAction action, Int32 initialCapacity)
        {
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier, initialCapacity + ERAUtils.BitManipulation.BytesToHold((Int32)MapAction.Max));
            msg.WriteRangedInteger(0, (Int32)MapAction.Max, (Int32)action);
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="interactable"></param>
        internal void InteractableJoined(ObjectId mapId, Data.Interactable interactable)
        {
            Data.AI.InteractableAppearance appearance = (Data.AI.InteractableAppearance)interactable.GetComponent(typeof(Data.AI.InteractableAppearance));

            NetOutgoingMessage msg = OutgoingMessage(MapAction.InteractableJoined);
            msg.Write(interactable.Id.ToByteArray());
            msg.Write(mapId.ToByteArray());

            // In case already downloaded TODO: remove when redundant
            msg.Write(interactable.MapX);
            msg.Write(interactable.MapY);
            msg.Write(appearance.MapDir);

            this.QueueAction(() => {
                if (_initialized)
                {
                    Logger.Debug("connection: " + this.Connection.NetConnection.RemoteEndpoint.ToString());
                    this.Connection.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
                }
                else
                {
                    _preInitializationMessage.Add(msg);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="interactable"></param>
        internal void InteractableLeft(ObjectId mapId, Data.Interactable interactable)
        {
            NetOutgoingMessage msg = OutgoingMessage(MapAction.InteractableLeft);
            msg.Write(interactable.Id.ToByteArray());
            msg.Write(mapId.ToByteArray());

            this.QueueAction(() =>
            {
                if (_initialized)
                {
                    this.Connection.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
                }
                else
                {
                    _preInitializationMessage.Add(msg);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        internal void ExitedMap()
        {
            _initialized = false;
        }


    }
}
