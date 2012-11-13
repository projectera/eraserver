using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectERA.Protocols;
using Lidgren.Network;
using MongoDB.Bson;
using ERAUtils.Logger;

namespace ERAServer.Protocols.Client
{
    internal partial class Asset : Protocol
    {
        private const Int32 PARTIALSIZE = 1000;

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
            get { return (Byte)ClientProtocols.Asset; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection">Connection to bind to</param>
        /// <param name="networkManager"></param>
        public Asset(Connection connection)
            : base(connection)
        {

        }

        /// <summary>
        /// Processes Incoming Message
        /// </summary>
        /// <param name="msg"></param>
        internal override void IncomingMessage(Lidgren.Network.NetIncomingMessage msg)
        {
            AssetAction action = (AssetAction)msg.ReadRangedInteger(0, (Int32)AssetAction.Max);
            msg.SkipPadBits();

            switch (action)
            {
                case AssetAction.Get:
                    ObjectId getOneTimeKey = new ObjectId(msg.ReadBytes(12));
                    ERAUtils.Enum.AssetType getType = (ERAUtils.Enum.AssetType)msg.ReadUInt16();
                    String getName = msg.ReadString();
                    Data.Asset getAsset;

                    this.QueueAction(() =>
                    {
                        ERAUtils.Enum.AssetOperationResult getAssetResult = Data.Asset.GetFile(getType, getName, out getAsset);

                        if (getAssetResult == ERAUtils.Enum.AssetOperationResult.InUse)
                        {

                        }

                        NetOutgoingMessage getMessage = OutgoingMessage(AssetAction.Get, 13);
                        getMessage.Write(getOneTimeKey.ToByteArray());
                        getMessage.Write((Byte)getAssetResult);

                        Logger.Debug("Get asset " + getName.ToString() + (getAsset != null ? "/" + getAsset.RemoteFileName : String.Empty) + " to client w/key " + getOneTimeKey.ToString());

                        if (getAssetResult == ERAUtils.Enum.AssetOperationResult.Ok)
                            getAsset.Pack(getMessage);

                        // Send the message
                        this.Connection.SendMessage(getMessage, NetDeliveryMethod.ReliableUnordered);

                    });
                    break;

                case AssetAction.GetChunk:
                    ObjectId getChunkOneTimeKey = new ObjectId(msg.ReadBytes(12));
                    ERAUtils.Enum.AssetType getChunkType = (ERAUtils.Enum.AssetType)msg.ReadUInt16();
                    ObjectId getChunkId = new ObjectId(msg.ReadBytes(12));
                    BsonBinaryData getChunk;

                    this.QueueAction(() =>
                        {
                            ERAUtils.Enum.AssetOperationResult getChunkAssetResult = Data.Asset.GetChunk(getChunkType, getChunkId, out getChunk);

                            Int32 partialLength = PARTIALSIZE, iteration = 0;
                            Int32 bytesremaining = (getChunkAssetResult == ERAUtils.Enum.AssetOperationResult.Ok ? getChunk.Bytes.Length : 1);

                            while(bytesremaining > 0)
                            {
                                NetOutgoingMessage getChunkMessage = OutgoingMessage(AssetAction.GetChunk, 25 + (getChunk == null ? 0 : 4 + 4 + Math.Min(partialLength, bytesremaining)));
                                getChunkMessage.Write(getChunkOneTimeKey.ToByteArray());
                                getChunkMessage.Write(getChunkId.ToByteArray());
                                getChunkMessage.Write((Byte)getChunkAssetResult);

                                if (getChunkAssetResult == ERAUtils.Enum.AssetOperationResult.Ok)
                                {
                                    getChunkMessage.Write(iteration); // Partial Id

                                    // Get Data
                                    Int32 partialStart = iteration * partialLength;
                                    Int32 messageLength = Math.Min(partialLength, bytesremaining);
                                    Byte[] partialData = new Byte[messageLength];
                                    Array.Copy(getChunk.Bytes, partialStart, partialData, 0, messageLength);

                                    // Write Data
                                    getChunkMessage.Write(messageLength);

                                    if (messageLength > 0)
                                        getChunkMessage.Write(partialData);

                                    bytesremaining -= messageLength;
                                    iteration++;

                                    Logger.Verbose("Sending iteration for chunk " + getChunkId + ": " + iteration + "/r: " + bytesremaining);
                                } else {
                                    bytesremaining = 0;
                                }

                                // Send the message
                                this.Connection.SendMessage(getChunkMessage, NetDeliveryMethod.ReliableUnordered);
                            }
                        });

                    break;

                case AssetAction.Download:
                    ObjectId downloadOneTimeKey = new ObjectId(msg.ReadBytes(12));
                    ERAUtils.Enum.AssetType downloadType = (ERAUtils.Enum.AssetType)msg.ReadUInt16();
                    ObjectId downloadAssetId = new ObjectId(msg.ReadBytes(12));
                    ObjectId[] downloadChunks;


                    Int32 chunkSize, length;

                    this.QueueAction(() =>
                        {
                            ERAUtils.Enum.AssetOperationResult downloadAssetResult = Data.Asset.GetChunks(downloadType, downloadAssetId, out downloadChunks, out chunkSize, out length);

                            NetOutgoingMessage downloadMessage = OutgoingMessage(AssetAction.Download, 13 + (downloadChunks == null ? 0 : downloadChunks.Length * 12 + 4));
                            downloadMessage.Write(downloadOneTimeKey.ToByteArray());
                            downloadMessage.Write((Byte)downloadAssetResult);

                            Logger.Debug("Download asset " + downloadAssetId.ToString() + " to client w/key " + downloadOneTimeKey.ToString());

                            if (downloadAssetResult == ERAUtils.Enum.AssetOperationResult.Ok)
                            {
                                downloadMessage.Write(chunkSize);
                                downloadMessage.Write(PARTIALSIZE);
                                downloadMessage.Write(length);
                                downloadMessage.Write(downloadChunks.Length);
                                foreach (var chunk in downloadChunks)
                                    downloadMessage.Write(chunk.ToByteArray());
                            }
                            else
                            {

                            }

                            // Send the message
                            this.Connection.SendMessage(downloadMessage, NetDeliveryMethod.ReliableUnordered);

                        });

                    break;
            }

        }

        /// <summary>
        /// Creates an Outgoing Message with the specified action integer written to it.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        internal NetOutgoingMessage OutgoingMessage(AssetAction action)
        {
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier);
            msg.WriteRangedInteger(0, (Int32)AssetAction.Max, (Int32)action);
            msg.WritePadBits();
            return msg;
        }

        /// <summary>
        /// Creates an Outgoing Message with the specified action integer written to it.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private NetOutgoingMessage OutgoingMessage(AssetAction action, Int32 initialCapacity)
        {
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier, initialCapacity + ERAUtils.BitManipulation.BytesToHold((Int32)AssetAction.Max));
            msg.WriteRangedInteger(0, (Int32)AssetAction.Max, (Int32)action);
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
    }
}
