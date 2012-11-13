using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Lidgren.Network;
using Lidgren.Network.Authentication;
using MongoDB.Bson;
using PlayerService.Protocols;

namespace PlayerService.Connections
{
    /// <summary>
    /// 
    /// </summary>
    internal class ClientConnection : IDisposable
    {
        private Protocol[] _protocols = new Protocol[ProtocolConstants.NetworkMaxValue+1];
        private Boolean _connected, _disposed;
        private INetEncryption _netEncryption;
        private List<Int32> _unreliableChannels;

        private Timer _keepAliveTimer;

        /// <summary>
        /// Lidgren NetConnection (Pipe)
        /// </summary>
        public NetConnection NetConnection { get; protected set; }

        /// <summary>
        /// Local Lidgrend NetPeer (Origin)
        /// </summary>
        public NetPeer NetManager { get; protected set; }

        /// <summary>
        /// Node id
        /// </summary>
        public ObjectId NodeId { get; set; }

        /// <summary>
        /// Node username
        /// </summary>
        public String Username { get; set; }

        /// <summary>
        /// Connected flag
        /// </summary>
        public Boolean IsConnected
        {
            get { return _connected; }
        }

        /// <summary>
        /// Disposed flag
        /// </summary>
        public Boolean IsDisposed
        {
            get { return _disposed; }
        }

        /// <summary>
        /// Transfer flag
        /// </summary>
        public Boolean IsTransfering
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="manager">Local NetPeer</param>
        /// <param name="con">NetConnection</param>
        /// <param name="key">Encryption key</param>
        /// <param name="nodeId">Node Id</param>
        public ClientConnection(NetPeer manager, NetConnection con, INetEncryption encryption, ObjectId nodeId)
        {
            _keepAliveTimer = new System.Timers.Timer(5000);
            _keepAliveTimer.Elapsed += new ElapsedEventHandler((Object state, ElapsedEventArgs args) => 
                this.SendMessage(this.NetManager.CreateMessage(0), NetDeliveryMethod.Unreliable));

            this.Username = (con.Tag as Handshake).Username;
            this.NetManager = manager;
            this.NetConnection = con;
            this.NetConnection.Tag = this;
            this.NodeId = nodeId;

            if (encryption != null)
            {
                _netEncryption = encryption;
            }
            else
            {
                this.IsTransfering = true;
                _netEncryption = new NetXtea(new Byte[16]);
            }

            _unreliableChannels = new List<Int32>();
            for (Int32 i = 0; i < 32; i++)
                _unreliableChannels.Add(i);


            // Not connected until everything is done.
            System.Threading.Thread.MemoryBarrier();

            _connected = true;
            _keepAliveTimer.Start();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="manager">Local NetPeer</param>
        /// <param name="con">NetConnection</param>
        /// <param name="key">Node Id</param>
        public ClientConnection(NetPeer manager, NetConnection con, Byte[] key)
            : this(manager, con, key != null ? new NetXtea(key) : null, ObjectId.Empty)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="con"></param>
        /// <param name="encryption"></param>
        public ClientConnection(NetPeer manager, NetConnection con, INetEncryption encryption)
            : this(manager, con, encryption, ObjectId.Empty)
        {

        }

        /// <summary>
        /// Transfer constructor
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="con"></param>
        public ClientConnection(NetPeer manager, NetConnection con, ObjectId id)
            : this(manager, con, null, id) { }

        /// <summary>
        /// Register Protocol
        /// </summary>
        /// <param name="protocol">Protocol</param>
        public void RegisterProtocol(Protocol protocol)
        {
            if (protocol.ProtocolIdentifier > ProtocolConstants.NetworkMaxValue || protocol.ProtocolIdentifier < 0)
            {
                //Logger.Error("Invalid protocolIdentifier during protocol registration");
                return;
            }

            if (_protocols[protocol.ProtocolIdentifier] != null)
            {
                //Logger.Warning("Trying to register a protocol that is already registered");
                return;
            }

            _protocols[protocol.ProtocolIdentifier] = protocol;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="protocol"></param>
        public Boolean TryGetProtocol(Type type, out Protocol protocol)
        {
            lock(_protocols)
                protocol = _protocols.First((p) => p != null && p.GetType().Equals(type));

            return protocol != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="protocolIdentifier"></param>
        /// <param name="protocol"></param>
        public Boolean TryGetProtocol(Byte protocolIdentifier, out Protocol protocol)
        {
            lock (_protocols)
                protocol = _protocols[protocolIdentifier];

            return protocol != null;
        }

        /// <summary>
        /// This handles an incoming message 
        /// </summary>
        /// <param name="msg">The received message</param>
        public void IncomingMessage(NetIncomingMessage msg)
        {
            if (!this.IsConnected)
                return;

            // Is this is a heartbeat message?
            if (msg.LengthBits == 0)
                return;

            msg.Decrypt(_netEncryption);

            if (msg.LengthBits == 0)
                return;

            Byte p = msg.ReadByte();

            if (p == (Byte)ProtocolConstants.ExtensionByte)
            {
                ExtendedIncomingMessage(msg);
                return;
            }

            if (_protocols[p] == null)
            {
                Error(new NetException("Invalid protocol Id received"));
                return;
            }

            _protocols[p].IncomingMessage(msg);
        }

        /// <summary>
        /// This handles the extended protocols
        /// </summary>
        /// <param name="msg">The received message</param>
        private void ExtendedIncomingMessage(NetIncomingMessage msg)
        {

        }

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="method">The delivery method</param>
        /// <param name="sequenceChannel">The sequence channel</param>
        public void SendMessage(NetOutgoingMessage msg, NetDeliveryMethod method, Int32 sequenceChannel)
        {
            msg.Encrypt(_netEncryption);
            NetConnection.SendMessage(msg, method, sequenceChannel);
        }

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="method">The delivery method</param>
        public void SendMessage(NetOutgoingMessage msg, NetDeliveryMethod method)
        {
            SendMessage(msg, method, 0);
        }

        /// <summary>
        /// Creates a new message for a protocol
        /// </summary>
        /// <param name="protocolIdentifier">The ids of the protocol</param>
        /// <returns>The created message</returns>
        public NetOutgoingMessage MakeMessage(Byte protocolIdentifier, Byte extensionByte, Int32 extensions)
        {
            NetOutgoingMessage msg = NetManager.CreateMessage();
            while(extensions-- > 0)
                msg.Write(extensionByte);
            msg.Write(protocolIdentifier);
            return msg;
        }

        /// <summary>
        /// Creates a new message for a protocol
        /// </summary>
        /// <param name="protocolIdentifier">The id of the protocol</param>
        /// <returns>The created message</returns>
        public NetOutgoingMessage MakeMessage(Byte protocolIdentifier)
        {
            return MakeMessage(protocolIdentifier, 0, 0);
        }

        /// <summary>
        /// Creates a new message for a protocol with an initial capacity.
        /// </summary>
        /// <param name="protocolIdentifier">The id of the protocol</param>
        /// <param name="initialCapacity">The size in bytes of the created message</param>
        /// <returns>The created message</returns>
        public NetOutgoingMessage MakeMessage(Byte protocolIdentifier, Int32 initialCapacity)
        {
            NetOutgoingMessage msg = NetManager.CreateMessage(initialCapacity);
            msg.Write(protocolIdentifier);
            return msg;
        }

        /// <summary>
        /// Returns a channel you can use to send unreliable sequenced data
        /// </summary>
        /// <returns>The channel you can use</returns>
        public int RegisterUnreliableChannel()
        {
            lock (_unreliableChannels)
            {
                if (_unreliableChannels.Count == 0)
                {
                    //Try to keep working with reduced performance (Higher chance of wrong packed discards) or crash if in Debug mode
#if DEBUG
                    Error(new NetException("No more unreliable channels left to distribute"));
#else
                    //Refill channel list and redistribute already used channels.
                    Logger.Warning("No more unreliable channels left to distribute, redistributing existing channels!");

                    for (int i = 0; i < 32; i++)
                        _unreliableChannels.Add(i);
#endif
                }

                Int32 id = _unreliableChannels[0];
                _unreliableChannels.Remove(id);
                return id;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        public void ReleaseUnreliableChannel(int channel)
        {
            lock(_unreliableChannels)
                _unreliableChannels.Add(channel);
        }

        /// <summary>
        /// Logs the error and closes the connection
        /// </summary>
        /// <param name="e">The exception that took place.</param>
        public void Error(Exception e)
        {
            lock (_protocols)
            {
                for (Int32 i = 0; i < ProtocolConstants.NetworkMaxValue; i++)
                    if (_protocols[i] != null)
                        _protocols[i].ErrorCancelation.Cancel();

                if (!this.IsConnected)
                    return;

                //Logger.Error("An error occured on one of the threads (" + e.Message + ") at: " + e.Source);
                //Logger.Debug("Stacktrace: " + e.StackTrace);
                //Logger.Info("Closing connection");

                for (Int32 i = 0; i < ProtocolConstants.NetworkMaxValue; i++)
                    if (_protocols[i] != null)
                        _protocols[i].Disconnect();

                _connected = false;

                Dispose();
                NetConnection.Disconnect("An error occured");
            }
        }
  
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources. Will deregister all protocols
        /// </summary>
        public void Dispose()
        {
            if(this.IsDisposed)
                return;

            _keepAliveTimer.Stop();
            _disposed = true;

            for(Int32 i = 0; i < ProtocolConstants.NetworkMaxValue; i++)
                if (_protocols[i] != null)
                {
                    _protocols[i].Disconnect();
                    _protocols[i].DeRegister();
                    _protocols[i].Dispose();
                }

            _keepAliveTimer.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        internal void SetEncryption(INetEncryption enc)
        {
            _netEncryption = enc;
        }
    }
}
