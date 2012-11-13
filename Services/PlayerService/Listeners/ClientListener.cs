using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PlayerService.Protocols;
using Lidgren.Network;
using MongoDB.Bson;
using Lidgren.Network.Authentication;
using PlayerService;
using PlayerService.Connections;

namespace PlayerService.Listeners
{
    /// <summary>
    /// interfaces client <-> gameserver connections
    /// </summary>
    internal class ClientListener
    {
        internal static Int32 Port { get; private set; }
        internal Boolean IsRunning { get; set; }
        
        private Thread _serverThread;
        private NetServer _server;

        public NetServer Server { get { return _server; } }

        private TimeSpan ReleasePrematureMessageAfter = TimeSpan.FromSeconds(1);

        public delegate void ConnectionDelegate(ObjectId nodeId, String username);
        public event ConnectionDelegate OnConnected = delegate { };
        public event ConnectionDelegate OnDisconnected = delegate { };

        //public static ConcurrentDictionary<ObjectId, UserTransferData> TransferedUsers = new ConcurrentDictionary<ObjectId, UserTransferData>();
        //public static ConcurrentDictionary<ObjectId, UserTransferData> PendingUserTransfers = new ConcurrentDictionary<ObjectId, UserTransferData>();

        /// <summary>
        /// Client Listener
        /// </summary>
        public ClientListener()
        {
            // Configuration
            NetPeerConfiguration config = new NetPeerConfiguration("ERA.Client");
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.Port = 15936;
            config.ConnectionTimeout = 60 * 15;
            config.UseMessageRecycling = true;

            _server = new NetServer(config);
            _server.Start();

            ClientListener.Port = config.Port;
        }

        /// <summary>
        /// Start running thread
        /// </summary>
        public void Start()
        {
            _serverThread = new Thread(new ThreadStart(ClientLoop));
            _serverThread.Name = "Client thread";
            this.IsRunning = true;
            _serverThread.Start();
        }

        /// <summary>
        /// Client message loop
        /// </summary>
        private void ClientLoop()
        {
            NetIncomingMessage msg;
            
            while (this.IsRunning)
            {
                _server.MessageReceivedEvent.WaitOne();
                msg = _server.ReadMessage();

                // If this second no messages accepted, releave CPU
                if (msg == null)
                    continue;

                try
                {
                    switch (msg.MessageType)
                    {
                        // MESSAGETYPE: DATA
                        // The main message type in networking is the data type. When the connection is not linked, the
                        // data is the verification data of the handshake and will be processed accordingly. If not, 
                        // the message is passed onto the Connection and processed by their respective protocol.
                        case NetIncomingMessageType.Data:

                            // Not authenticated? Maybe we are just in the transfer protocol, but we didn't connect yet.
                            // It got JUST a little bit too early.

                            // TODO Rewrite the following code:
                            /*if ((!msg.SenderConnection.IsUsingSRP && msg.SenderConnection.Tag is Connection && ((Connection)msg.SenderConnection.Tag).IsTransfering) || 
                                msg.SenderConnection.Tag == null || msg.SenderConnection.IsConnectingSRP)
                            {
                                Logger.Info("We received data júst a little too early. Releasing premature data in a bit.");

                                Task.Factory.StartNew(() =>
                                {
                                    Thread.Sleep(ReleasePrematureMessageAfter);

                                    msg.Position = 0;
                                    _server.ReleaseMessage(msg);
                                });
                                return;
                            }
                            else if (!msg.SenderConnection.IsUsingSRP)
                            {
                                msg.SenderConnection.Disconnect("Not authenticated!");
                                return;
                            }*/

                            var connection = msg.SenderConnection.Tag as ClientConnection;
                            if (connection != null)
                            {
                                connection.IncomingMessage(msg);
                            }
                            else
                            {
                                var handshake = Lidgren.Network.Lobby.NetLobby.IncomingMessage(msg);

                                switch (handshake)
                                {
                                    case Handshake.Contents.Succes:

                                        ClientConnection new_connection = new ClientConnection(_server, msg.SenderConnection, (msg.SenderConnection.Tag as Handshake).CreateEncryption());
                                        RegisterProtocols(new_connection, connection.Username);

                                        OnConnected.Invoke(new_connection.NodeId, new_connection.Username);
                                        //Logger.Info("SRP connection established with: " + msg.SenderConnection.RemoteEndpoint);
                                        break;

                                    case Handshake.Contents.Error:
                                    case Handshake.Contents.Denied:
                                        msg.SenderConnection.Disconnect("Error occured during handshake.");
                                        //Logger.Error("Error occured during handshake.");
                                        break;
                                    case Handshake.Contents.Expired:
                                        msg.SenderConnection.Disconnect("Handshake expired.");
                                        //Logger.Info("Handshake expired");
                                        break;
                                }
                            }

                            break;

                        // MESSAGETYPE: CONNECTION APPROVAL
                        // The ConnectionApproval message type is seen when a node yields the peer#connect function. When
                        // the RemoteEndpoint specified is reached, a loose connection is made. It's up to the other end,
                        // the one that is connected too, to deny or approve the connection. 
                        case NetIncomingMessageType.ConnectionApproval:

                            // SRPP(username, A) data
                            Console.WriteLine("Connection request from " + msg.SenderEndPoint.Address);

                            // No SRP is only allowed if transfering

                            // TODO: Rewrite the following code:
                            /*if (!msg.SenderConnection.IsConnectingSRP)
                            {
                                try
                                {
                                    // Ah, we got haildata! ObjectId I pressume
                                    
                                    ObjectId id = new ObjectId(msg.ReadBytes(12));

                                    UserTransferData value;
                                    if (TransferedUsers.TryGetValue(id, out value))
                                    {
                                        // Probably start transfer directly
                                        Connection connection = new Connection(_server, msg.SenderConnection, id);
                                        msg.SenderConnection.Approve();

                                        Logger.Info("Transfering user " + id);
                                        break;
                                    } 
                                    else 
                                    {
                                        
                                        msg.SenderConnection.Deny("No transfer data found.");
                                        break;
                                    }
                                }
                                catch
                                {
                                    msg.SenderConnection.Deny("Error while trying to find transfer data.");
                                    break;
                                }
                            }*/

                            msg.SenderConnection.Approve();
                        
                            break;

                        // MESSAGETYPE: HANDSHAKEMESSAGE
                        // Contains the Reason byte and information about that reason~message. Occurs when Handshake fails,
                        // is denied, succeeds or expires.

                        // TODO: Remove the following code:
                        /*
                        case NetIncomingMessageType.HandshakeMessage:
                            HandshakeMessageReason reason = (HandshakeMessageReason)msg.ReadByte();
                            switch (reason)
                            {
                                case HandshakeMessageReason.Error:
                                case HandshakeMessageReason.Expired:
                                    // Reconnect?
                                    break;

                                case HandshakeMessageReason.Denied:
                                case HandshakeMessageReason.Denied | HandshakeMessageReason.Username:
                                case HandshakeMessageReason.Denied | HandshakeMessageReason.Password:
                                    // This shouldn't happen
                                    break;

                                case HandshakeMessageReason.Succes:
                                    // awesome
                                    Connection connection = new Connection(_server, msg.SenderConnection, msg.SenderConnection.CreateEncryption());
                                    RegisterProtocols(connection, msg.SenderConnection.ConnectingUsername);

                                    Logger.Info("SRP connection established with: " + msg.SenderConnection.RemoteEndpoint);

                                    break;
                            }
                            break;
                            */

                        // MESSAGETYPE: STATUS CHANGED
                        // Internal type that is triggered when a connection is initiated, responded too, connecting,
                        // disconnecting, connected or disconnected. Upon a connection, we might have received some
                        // RemoteHailData. This is part of the SRPP protocol and is proccesed accordingly. When
                        // disconnecting, the Connection is disposed, internal connection is disconnected and all is
                        // logged. 
                        case NetIncomingMessageType.StatusChanged:

                            NetConnectionStatus statusByte = (NetConnectionStatus)msg.ReadByte();
                            //Logger.Debug("ConnectionStatus: " + statusByte + " for " + msg.SenderEndpoint.ToString());

                            switch (statusByte)
                            {
                                case NetConnectionStatus.Disconnecting:
                                    //Logger.Debug("Disconnecting from " + msg.SenderEndpoint.Address.ToString());
                                    break;

                                // When disconnect is called and processed
                                case NetConnectionStatus.Disconnected:
                                    // If already connection established, destroy resources
                                    var disconnected_connection = msg.SenderConnection.Tag as ClientConnection;
                                    if (disconnected_connection != null)
                                    {
                                        OnDisconnected.Invoke(disconnected_connection.NodeId, disconnected_connection.Username);
                                        if (!disconnected_connection.IsDisposed)
                                            disconnected_connection.Dispose();
                                    }

                                    // Received a reason for disconnecting? (e.a. Handshake Fail)
                                    String finalReason = Encoding.UTF8.GetString(msg.ReadBytes((Int32)msg.ReadVariableUInt32()));
                                    //Logger.Debug("Disconnected from " + msg.SenderConnection.RemoteEndpoint.Address.ToString());
                                    //Logger.Debug("-- reason " + finalReason);
                                    break;

                                // When connected, check for SRP usage. If not, we are probably transfering
                                case NetConnectionStatus.Connected:
                                    // We are transfering? Complete transfer

                                    // TODO: Rewrite the following code:
                                    /*if (!msg.SenderConnection.IsUsingSRP)
                                        if (msg.SenderConnection.Tag is Connection)
                                            if (((Connection)msg.SenderConnection.Tag).IsTransfering)
                                            {
                                                UserTransferData data;
                                                if (TransferedUsers.TryRemove(((Connection)msg.SenderConnection.Tag).NodeId, out data))
                                                {
                                                    Connection conn = ((Connection)msg.SenderConnection.Tag);
                                                    conn.IsTransfering = false;
                                                    conn.SetEncryption(msg.SenderConnection.SetSRPKey(data.Username, data.SessionKey));

                                                    // Register protocols
                                                    RegisterProtocols(conn, data.Username);

                                                    // Set active id
                                                    Protocol pprotocol;
                                                    Player playerProtocol;
                                                    conn.TryGetProtocol((Byte)ClientProtocols.Player, out pprotocol);
                                                    playerProtocol = (Player)pprotocol;
                                                    playerProtocol.ActiveId = data.ActiveId;

                                                    Logger.Info("Transfer connection established.");// with: " + msg.SenderConnection.RemoteEndpoint);

                                                    // Issue map join
                                                    playerProtocol.EnterMap();

                                                    // Send transfer ack
                                                    conn.SendMessage(playerProtocol.PickAvatarResponse(), NetDeliveryMethod.ReliableUnordered);

                                                }
                                                else
                                                {
                                                    // ERROR!
                                                    msg.SenderConnection.Disconnect("No TransferData found!");
                                                }
                                            }
                                    */
                                    break;

                                default:
                                    String statusChange = Encoding.UTF8.GetString(msg.ReadBytes((Int32)msg.ReadVariableUInt32()));
                                    //Logger.Debug("-- reason " + statusChange);
                                    break;
                            }

                            break;

                            #if DEBUG
                            case NetIncomingMessageType.DebugMessage:
                                String debugMessage = Encoding.UTF8.GetString(msg.ReadBytes((Int32)msg.ReadVariableUInt32()));
                                //Logger.Verbose("INT " + debugMessage);
                                break;
                            #endif

                            case NetIncomingMessageType.WarningMessage:
                                String warningMessage = Encoding.UTF8.GetString(msg.ReadBytes((Int32)msg.ReadVariableUInt32()));
                                //Logger.Verbose("INT " + warningMessage);
                                break;

                            case NetIncomingMessageType.ErrorMessage:
                                String errorMessage = Encoding.UTF8.GetString(msg.ReadBytes((Int32)msg.ReadVariableUInt32()));
                                //Logger.Verbose("INT " + errorMessage);
                                break;

                            default:
                                throw new NetException("MessageType: " + msg.MessageType + " is not supported.");
                        }

                    // Recycle please
                    _server.Recycle(msg);
                }
                catch(Exception e) 
                {
                    try
                    {
                        msg.SenderConnection.Disconnect("No tolerance: exception " + e.Message);
#if DEBUG
                        //throw;
#endif
                    }
                    catch (Exception)
                    {
#if DEBUG
                        throw;
#endif
                    }
                }
            }
            
            _server.Shutdown("Server shutting down");

            Console.WriteLine("Client Service is Stopping at " + NetTime.ToReadable(NetTime.Now));

            _server.Shutdown("Final shutdown");
        }

        /// <summary>
        /// Adds the protocols for this type of server to a new connection
        /// </summary>
        /// <param name="connection">The connection to add the protocols to</param>
        public void RegisterProtocols(ClientConnection connection, String username)
        {
            /*Protocol pp = new Player(connection, username);
            connection.RegisterProtocol(pp);

            Protocol pi = new Interactable(connection);
            connection.RegisterProtocol(pi);

            Protocol pm = new Map(connection);
            connection.RegisterProtocol(pm);

            Protocol pg = new Guild(connection);
            connection.RegisterProtocol(pg);

            Protocol pa = new Asset(connection);
            connection.RegisterProtocol(pa);*/

            //Logger.Info("Protocols registered for connection " + connection.NetConnection.RemoteEndpoint.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class AuthenticatedUser
    {
        internal ObjectId UserID { get; set; }
        internal String Username { get; set; }
        internal Byte[] SessionKey { get; set; }
    } 
}
