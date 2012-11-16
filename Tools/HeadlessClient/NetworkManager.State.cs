using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network.Authentication;
using System.Net;
using Lidgren.Network;
using System.Threading;
using Lidgren.Network.Lobby;
using System.Threading.Tasks;

namespace ERA.Tools.HeadlessClient
{
    internal partial class NetworkManager 
    {
        private String _username, _password;
        private Handshake _handshake;
        private WebClient _webClient;
        private IPEndPoint _nearbyServer;
        private AuthenticationStatus _authenticationStep;
        private Int32 _reconnectCounter;
        private NetClient _transferClient;

        public delegate void AuthenticationSucces(Connection connection);
        public delegate void AuthenticationFailure(String reason);
        public delegate void AuthenticationProgress(AuthenticationStatus step);
        public event AuthenticationSucces OnAuthenticated;
        public event AuthenticationFailure OnAuthenticationFailed;
        public event AuthenticationFailure OnAuthenticationDenied;
        public event AuthenticationProgress OnAuthenticationStep;
        public event AuthenticationFailure OnAuthenticationTimeout;


        public const String ServerRetrieveAddress = "http://projectera.org/server/get.php";
        public const Int32 MaxReconnectRounds = 0;

        /// <summary>
        /// 
        /// </summary>
        public void Loop()
        {
            try
            {
                // This is checked each cycle
                while (this.IsRunning)
                {
                    _client.MessageReceivedEvent.WaitOne(1000);
                    NetIncomingMessage msg = _client.ReadMessage();

                    // No message received, please poll
                    if (msg == null)
                        continue;

                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            if (msg.SenderConnection.Tag is Connection)
                                ((Connection)msg.SenderConnection.Tag).IncomingMessage(msg);
                            else
                                NetLobby.IncomingMessage(msg);
                            break;

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
                                    if (_transferClient != null && _transferClient.ConnectionStatus == NetConnectionStatus.Connected)
                                    {
                                        //if (_client.ConnectionStatus == NetConnectionStatus.Connected)
                                            //Logger.Error("Client still connected, but transfer is taking place now.");

                                        //Logger.Info("Transfer from " + msg.SenderConnection.RemoteEndpoint + " to " + _transferClient.ServerConnection.RemoteEndpoint + " completed.");

                                        //_transferClient.SaveTransferConnection(ref _client);          // merge pools and statistics
                                        _connection.NetConnection = _transferClient.ServerConnection;   // move active NetConnection
                                        _connection.NetConnection.Tag = _connection;                    // move active Connection
                                        _client = _transferClient;                                      // move active NetPeer
                                        _transferClient = null;
                                        break;
                                    }

                                    // If already connection established, destroy resources
                                    if (msg.SenderConnection.Tag is Connection &&
                                        !((Connection)msg.SenderConnection.Tag).IsDisposed)
                                        ((Connection)msg.SenderConnection.Tag).Dispose();

                                    // Received a reason for disconnecting? (e.a. Handshake Fail)
                                    String finalReason = Encoding.UTF8.GetString(msg.ReadBytes((Int32)msg.ReadVariableUInt32()));
                                    //Logger.Debug("Disconnected from " + msg.SenderConnection.RemoteEndpoint.Address.ToString());
                                    //Logger.Debug("-- reason " + finalReason);
                                    if (finalReason.StartsWith("Handshake data validation failed"))
                                    {
                                        SetStep(AuthenticationStatus.NoServerConnection);
                                        if (OnAuthenticationFailed != null)
                                            OnAuthenticationFailed.Invoke("Could not connect");
                                    }
                                    else if (finalReason.StartsWith("Failed to establish"))
                                    {
                                        SetStep(AuthenticationStatus.NoServerConnection);
                                        if (OnAuthenticationTimeout != null)
                                            OnAuthenticationTimeout.Invoke("Could not connect");

                                    }
                                    break;

                                case NetConnectionStatus.Connected:
                                    SetStep(AuthenticationStatus.ServerConnection);

                                    var username = _username;
                                    var password = _password;

                                    Authenticate(_client.ServerConnection, _username, _password);
                                    break;
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //Logger.Fatal(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public void RegisterProtocols(Connection connection)
        {
            Protocol pp = new Protocols.Player(connection, this);
            connection.RegisterProtocol(pp);

            //Protocol pi = new Protocols.Interactable(connection, this);
            //connection.RegisterProtocol(pi);

            //Protocol pm = new Protocols.Map(connection, this);
            //.RegisterProtocol(pm);

            //Protocol pa = new Protocols.Asset(connection, this);
            //connection.RegisterProtocol(pa);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetupLobby()
        {
            NetLobby.OnDenied += new NetLobby.HandshakeFinishedEvent(NetLobby_OnDenied);
            NetLobby.OnError += new NetLobby.HandshakeFinishedEvent(NetLobby_OnError);
            NetLobby.OnExpired += new NetLobby.HandshakeFinishedEvent(NetLobby_OnExpired);
            NetLobby.OnSucces += new NetLobby.HandshakeFinishedEvent(NetLobby_OnSucces);

            NetLobby.KeySize = NetworkManager.keySize;
        }

        #region Events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reason"></param>
        void NetLobby_OnSucces(string reason)
        {
            //Logger.Info(reason);

            _connection = new Connection(_client, _client.ServerConnection, (_client.ServerConnection.Tag as Handshake).CreateEncryption());
            RegisterProtocols(_connection);
            try
            {
                OnAuthenticated.Invoke(_connection);
                SetStep(AuthenticationStatus.Authenticated);
            }
            catch (InvalidOperationException)
            {
                OnAuthenticationFailed.Invoke("Error occured while creation Connection");
                SetStep(AuthenticationStatus.HandshakeFailed);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reason"></param>
        void NetLobby_OnExpired(string reason)
        {
            //Logger.Info(reason);
            SetStep(AuthenticationStatus.HandshakeExpired);

            var username = _username;
            var password = _password;

            Authenticate(_client.ServerConnection, _username, _password);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reason"></param>
        void NetLobby_OnError(string reason)
        {
            //Logger.Error(reason);
            OnAuthenticationFailed.Invoke(reason);

            Disconnect("Error during handshake: " + reason);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reason"></param>
        void NetLobby_OnDenied(string reason)
        {
            //Logger.Info(reason);
            OnAuthenticationDenied.Invoke(reason);
            Disconnect("User denied " + reason);
        }
        #endregion

        /// <summary>
        /// Starts connecting with username and password
        /// </summary>
        /// <param name="username">username</param>
        /// <param name="password">password</param>
        public void AsyncConnect(String username, String password)
        {
            if (_authenticationStep == AuthenticationStatus.None || AuthenticationStatus.CanConnect.HasFlag(_authenticationStep))
            {
                // Reset reconnection counter
                _reconnectCounter = 0;

                // Save credentials
                _username = username.ToLower().Trim();
                _password = password;

                // Reset nearby server
                _nearbyServer = null;

                // Find server to connect to
                AsyncFindNearbyServer();
            }
        }

        /// <summary>
        /// Cancells any running connections
        /// </summary>
        internal void CancelConnect()
        {
            if (_webClient != null && _webClient.IsBusy)
                _webClient.CancelAsync();

            if (_client != null)
                _client.Disconnect("Cancelled connecting");

            SetStep(AuthenticationStatus.Cancelled);
        }

        /// <summary>
        /// Starts finding a nearby server asynchronously
        /// </summary>
        public void AsyncFindNearbyServer(Boolean newServer)
        {
            if (_webClient != null && _webClient.IsBusy)
                return;

            SetStep(AuthenticationStatus.FindServer);

            // Start the download
            _webClient = new WebClient();
            _webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(_webClient_DownloadStringCompleted);
            _webClient.DownloadStringAsync(new Uri(ServerRetrieveAddress), newServer);
        }

        /// <summary>
        /// Async finds new server
        /// </summary>
        public void AsyncFindNearbyServer()
        {
            AsyncFindNearbyServer(false);
        }

        /// <summary>
        /// Event handler for completion of server retrieval
        /// </summary>
        /// <param name="sender">Source</param>
        /// <param name="e">Event arguments</param>
        private void _webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            // If cancelled by the user
            if (e.Cancelled)
            {
                SetStep(AuthenticationStatus.Cancelled);
            }
            // If error by the webclient
            else if (e.Error is Exception)
            {
#if DEBUG
                SetStep(AuthenticationStatus.ServerFound);

                IPAddress server = Lidgren.Network.NetUtility.Resolve("localhost");
                // Save the server and start the hand shake
                _nearbyServer = new IPEndPoint(server, ServerPort);
                Connect(_nearbyServer);
#else

               SetStep(AuthenticationStatus.NoServerFound);
#endif
            }
            // If succeeded
            else
            {
                SetStep(AuthenticationStatus.ServerFound);

                if (!String.IsNullOrWhiteSpace(e.Result))
                {
                    IPAddress server = null;
                    try
                    {
                        server = Lidgren.Network.NetUtility.Resolve(e.Result);
                    }
                    catch (Exception)
                    {

                    }

#if DEBUG
                    if (server == null)
                        server = Lidgren.Network.NetUtility.Resolve("localhost");
#endif

                    // NOTE doesn't work if there are 3 severs, with 2 unreachable. Fix it!
                    if (_nearbyServer != null && server.Equals(_nearbyServer.Address))
                    {
                        SetStep(AuthenticationStatus.NoServerConnection);

                        // Remove this from events
                        _webClient.DownloadStringCompleted -= _webClient_DownloadStringCompleted;

                        // Return early
                        return;
                    }

                    // Save the server and start the hand shake
                    if (server != null)
                    {
                        _nearbyServer = new IPEndPoint(server, ServerPort);
                        Connect(_nearbyServer);
                    }
                    else
                    {
                        SetStep(AuthenticationStatus.NoServerFound);
                    }
                }
                else
                {
                    SetStep(AuthenticationStatus.NoServerFound);
                }
            }

            // Remove this from events
            _webClient.DownloadStringCompleted -= _webClient_DownloadStringCompleted;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPoint"></param>
        public void Connect(IPEndPoint endPoint)
        {
            _client.Connect(endPoint);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void Authenticate(NetConnection connection, String username, String password)
        {
            if (!IsConnected)
            {
                Connect(_nearbyServer);
                return;
            }

            SetStep(AuthenticationStatus.ServerConnection);

            NetLobby.Authenticate(connection, username, password);
            _handshake = connection.Tag as Handshake;

            SetStep(AuthenticationStatus.HandshakeData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPoint"></param>
        public void Transfer(IPEndPoint endPoint)
        {
            // TODO: lot of checks

            //Logger.Info("Transfer requested to :" + endPoint);

            // Create configuration for new transfer
            NetPeerConfiguration config = _client.Configuration.Clone();
            config.Port = TransferPort;

            // Create TransferClient
            _transferClient = new NetClient(config);
            _transferClient.Start();

            // Start connecting
            NetOutgoingMessage hailMessage = _transferClient.CreateMessage();
            hailMessage.Write(_connection.NodeId);
            _transferClient.Connect(endPoint, hailMessage);

            // TODO: connection completion, transfer loop!
            Task.Factory.StartNew(() =>
            {

                NetIncomingMessage message;
                NetConnectionStatus status;

                // Wait for transfer client to connect
                do
                {
                    do
                    {
                        _transferClient.MessageReceivedEvent.Set();

                        while ((message = _transferClient.ReadMessage()) == null) ;
                    } while (message.MessageType != NetIncomingMessageType.StatusChanged);
                    status = (NetConnectionStatus)message.ReadByte();

                    //Logger.Debug("Transfer status: " + status);

                } while (status != NetConnectionStatus.Connected && status != NetConnectionStatus.Disconnected);

                // If disconnected, error must have occurred!
                if (status == NetConnectionStatus.Disconnected)
                {
                    //Logger.Error(message.ReadString());
                    return;
                }

                //Logger.Info("Transfer destination connected.");

                _transferClient.Tag = _client.Tag;
                _client.Disconnect("Transfer issued.");
                _client = null;
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        private void SetStep(AuthenticationStatus status)
        {
            if (_authenticationStep == status)
                return;

            _authenticationStep = status;

            if (OnAuthenticationStep != null)
                OnAuthenticationStep.Invoke(status);
        }

        /// <summary>
        /// Disconnects client and resets connecting status
        /// </summary>
        /// <param name="message">message to disconnect with</param>
        public void Disconnect(String message)
        {
            if (_client != null)
                _client.Disconnect(message);
            //if (_transferClient != null)
            //    _transferClient.Disconnect(message);

            _nearbyServer = null;

            SetStep(AuthenticationStatus.None);
        }

        /// <summary>
        /// 
        /// </summary>
        [Flags]
        public enum AuthenticationStatus
        {
            None = 0,

            FindServer = (1 << 0),
            ServerFound = (1 << 1),
            ServerConnection = (1 << 2),
            HandshakeData = (1 << 3),
            HandshakeVerification = (1 << 4),
            Authenticated = (1 << 5),

            HandshakeFailed = (1 << 6),
            HandshakeExpired = (1 << 7),
            HandshakeDenied = (1 << 8),

            NoServerFound = (1 << 9),
            NoServerConnection = (1 << 10),
            Cancelled = (1 << 11),

            CanConnect = NoServerFound | NoServerConnection | Cancelled | HandshakeFailed | HandshakeExpired | HandshakeDenied,

            IsAuthenticating = HandshakeData | HandshakeVerification,
            IsConnecting = FindServer | ServerFound,
            IsConnected = ServerConnection,
        }
    }
}