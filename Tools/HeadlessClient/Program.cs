using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network.Lobby;

namespace HeadlessClient
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
            Console.WriteLine("Error: {0}", reason);
            QueryDetails();
        }

        static void  _manager_OnAuthenticationDenied(string reason)
        {
 	        Console.WriteLine("Denied: {0}", reason);
            QueryDetails();
        }

        static void _manager_OnAuthenticationTimeout(string reason)
        {
            Console.WriteLine("Timeout: {0}", reason);
            QueryDetails();
        }

        static void _manager_OnAuthenticationFailed(string reason)
        {
            Console.WriteLine("Failed: {0}", reason);
            QueryDetails();
        }

        static void _manager_OnAuthenticationStep(NetworkManager.AuthenticationStatus step)
        {
            Console.WriteLine(step);
            if (step == NetworkManager.AuthenticationStatus.NoServerFound)
            {
                _manager.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 15936));

                Console.WriteLine("Sorry! Trying loopback:15936");
                _manager.IsRunning = false;
            }
        }

        static void _manager_OnAuthenticated(Connection connection)
        {
            Console.WriteLine("YEAH DONE");
        }

        static void QueryDetails()
        {
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
