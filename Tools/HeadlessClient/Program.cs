using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network.Lobby;
using ERA.Protocols.ClientProtocol;

namespace ERA.Tools.HeadlessClient
{
    class Program
    {
        static NetworkManager _manager = new NetworkManager();
        static void Main(string[] args)
        {
            _manager.OnAuthenticated += new NetworkManager.AuthenticationSucces(_manager_OnAuthenticated);
            _manager.OnAuthenticationStep += new NetworkManager.AuthenticationProgress(_manager_OnAuthenticationStep);
            _manager.OnAuthenticationFailed += new NetworkManager.AuthenticationFailure(_manager_OnAuthenticationFailed);
            _manager.OnAuthenticationTimeout += new NetworkManager.AuthenticationFailure(_manager_OnAuthenticationTimeout);
            _manager.OnAuthenticationDenied += new NetworkManager.AuthenticationFailure(_manager_OnAuthenticationDenied);
            _manager.Initialize();

            NetLobby.OnError += new NetLobby.HandshakeFinishedEvent(NetLobby_OnError);

            QueryDetails();

            while (_manager.IsRunning)
            {
                System.Threading.Thread.Sleep(1);
                System.Threading.Thread.MemoryBarrier();
            }

            Console.ReadKey();
        }

        static void NetLobby_OnError(string reason)
        {
            Console.WriteLine(">> Error: {0} <<", reason);
            QueryDetails();
        }

        static void  _manager_OnAuthenticationDenied(string reason)
        {
            Console.WriteLine(">> Denied: {0} <<", reason);
            QueryDetails();
        }

        static void _manager_OnAuthenticationTimeout(string reason)
        {
            Console.WriteLine(">> Timeout: {0} <<", reason);
            QueryDetails();
        }

        static void _manager_OnAuthenticationFailed(string reason)
        {
            Console.WriteLine(">> Failed: {0}", reason);
            QueryDetails();
        }

        static void _manager_OnAuthenticationStep(NetworkManager.AuthenticationStatus step)
        {
            Console.WriteLine(step);
            if (step == NetworkManager.AuthenticationStatus.NoServerFound)
            {
                _manager.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 15936));

                Console.WriteLine(">> Trying loopback:15936 <<");
            }
        }

        static void _manager_OnAuthenticated(Connection connection)
        {
            Console.WriteLine(">> Authenticated <<");

            Protocol protocol;
            _manager.TryGetProtocol((Byte)ClientProtocols.Player, out protocol);

            var playerTask = ((ERA.Tools.HeadlessClient.Protocols.Player)protocol).Get((player) =>
                {
                    Console.WriteLine("Hello {0}! You registered on {1}.", player.Username, player.RegistrationDate.ToShortDateString());
                }
            );
        }

        static void QueryDetails()
        {
            _manager.CancelConnect();
            Console.Write("Username: ");
            var username = Console.ReadLine().Replace("\n", "").Replace("\r","");
            Console.Write("Password: ");
            var password = String.Empty;
            var key = Console.ReadKey(true);
            while (key.Key != ConsoleKey.Enter)
            {
                password += key.KeyChar;
                key = Console.ReadKey(true);
            }
            Console.WriteLine();
            _manager.AsyncConnect(username, password);
        }
    }
}
