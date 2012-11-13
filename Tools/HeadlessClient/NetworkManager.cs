using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Lidgren.Network;

namespace HeadlessClient
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    internal partial class NetworkManager
    {
        #region Options
        internal const Int32 ServerPort = 15936;
        private const Int32 ClientPort = 15900;
        private const Int32 TransferPort = 15901;
        private const Int32 keySize = 1024;
        #endregion

        private Thread _networkThread;
        private NetClient _client;
        private Connection _connection;

        /// <summary>
        /// Is true when someone is authenticated
        /// </summary>
        public Boolean IsAuthenticated { get { return _connection != null && _authenticationStep == AuthenticationStatus.Authenticated; } } //&& _connection.NetConnection.IsUsingSRP; } }

        /// <summary>
        /// 
        /// </summary>
        public Boolean IsAuthenticating
        {
            get
            {
                return _authenticationStep != AuthenticationStatus.None &&
                    AuthenticationStatus.IsAuthenticating.HasFlag(_authenticationStep);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Boolean IsConnected { get { return _client != null && _client.ConnectionStatus == NetConnectionStatus.Connected; }}

        /// <summary>
        /// 
        /// </summary>
        public Boolean IsConnecting 
        { 
            get { 
                return _client != null && 
                    _authenticationStep != AuthenticationStatus.None &&
                    AuthenticationStatus.IsConnecting.HasFlag(_authenticationStep); 
            } 
        }

        /// <summary>
        /// Roundtrip 
        /// </summary>
        public Single RoundTrip { get { return ((_connection != null && _connection.NetConnection != null && _connection.NetConnection.Status != NetConnectionStatus.Disconnected) ? _connection.NetConnection.AverageRoundtripTime : 0); } }

        /// <summary>
        /// Connecting Username
        /// </summary>
        public String Username { get { return ((_connection != null && _connection.NetConnection != null) ? _username : String.Empty); } }

        /// <summary>
        /// Gets the running state of the NetworkLoop. When set to false, terminates loop.
        /// </summary>
        public Boolean IsRunning { get; set; }

        /// <summary>
        /// Constructor for this component
        /// </summary>
        internal NetworkManager()
        {
            SetupLobby();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="protocol"></param>
        public Boolean TryGetProtocol(Type type, out Protocol protocol)
        {
            return _connection.TryGetProtocol(type, out protocol);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="protocolIdentifier"></param>
        /// <param name="protocol"></param>
        public Boolean TryGetProtocol(Byte protocolIdentifier, out Protocol protocol)
        {
            return _connection.TryGetProtocol(protocolIdentifier, out protocol);
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public void Initialize()
        {
            // Create configuration (app identifier has to match servers)
            NetPeerConfiguration config = new NetPeerConfiguration("ERA.Client");
            config.ConnectionTimeout = 60 * 15;
            config.UseMessageRecycling = true;

            //config.Port = ClientPort;
            _client = new NetClient(config);
            _client.Start();

            // Event modifier and running stte
            this.IsRunning = true;

            // Create and start network thread
            _networkThread = new Thread(Loop);
            _networkThread.Name = "NetworkManager Loop Thread";
            _networkThread.IsBackground = true;
            _networkThread.Start();
        }

        /// <summary>
        /// When player protocol has got user info, set the id
        /// </summary>
        /// <param name="p"></param>
        internal void SetPlayerId(Byte[] id)
        {
            _connection.NodeId = id;
        }

        /// <summary>
        /// On Game Exiting, stop server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Game_Exiting(object sender, EventArgs e)
        {
            this.IsRunning = false;
        }
    }
}
