﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using MongoDB.Driver;
using System.Threading;
using PlayerService.Listeners;
using SubscriptionProtocol;
using Lidgren.Network;
using MongoDB.Bson;

namespace PlayerService
{
    partial class Program
    {
        /// <summary>
        /// Reference to the service client
        /// </summary>
        internal static ServiceClient EraSClient;
        internal static ClientListener EraListener;

        /// <summary>
        /// Network Information
        /// </summary>
        public static NetworkInfo NetworkInfo { get; protected set; }

        /// <summary>
        /// Service is Running
        /// </summary>
        public static Boolean IsRunning
        {
            set
            {
                if (value) StopRunningSemaphore.Reset();
                else StopRunningSemaphore.Set();
            }
        }

        /// <summary>
        /// Signals the thread if it can stop running
        /// </summary>
        private static ManualResetEvent StopRunningSemaphore { get; set; }

        /// <summary>
        /// Subscriptions
        /// </summary>
        public static Subscriptions PlayerSubscriptions { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<ObjectId, String> PlayerMapper { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Functions
            RegisterFunctions();

            // Connect to the cloud
            EraSClient = ServiceClient.Connect("Player", true);
            StopRunningSemaphore = new ManualResetEvent(true);

            // Save the network info
            PlayerSubscriptions = new Subscriptions(EraSClient);
            PlayerMapper = new Dictionary<ObjectId, string>();
            NetworkInfo = new ServiceProtocol.NetworkInfo(EraSClient);

            // Message Handlers
            EraSClient.MessageHandlers.Add(MessageType.Service, HandleMessages);
            Console.WriteLine("Connected with Id: {0}", EraSClient.ServiceName);

            MapRunningPlayers();

            // Start running this
            Lidgren.Network.Lobby.NetLobby.LogonManager = new LogonManager("There is no secret.", Lidgren.Network.Lobby.NetLobby.KeySize);
            EraListener = new ClientListener();
            EraListener.OnConnected += new ClientListener.ConnectionDelegate(EraListener_OnConnected);
            EraListener.OnDisconnected += new ClientListener.ConnectionDelegate(EraListener_OnDisconnected);

            EraListener.Start();

            IsRunning = true;

            StopRunningSemaphore.WaitOne();
            Console.WriteLine("Service terminated.");
        }

        /// <summary>
        /// 
        /// </summary>
        public static void MapRunningPlayers()
        {
            var playerservices = NetworkInfo.GetServiceInstances("Player");
            // TODO make a broadcast type
            foreach (var playerservice in playerservices)
            {
                if (playerservice == NetworkInfo.Client.Identifier)
                    continue;

                try
                {
                    var question = EraSClient.CreateQuestion(MessageType.Internal, playerservice);
                    question.Packet.Write("GetRunning");
                    var answer = EraSClient.AskQuestion(question);
                    var count = answer.Packet.ReadInt32();
                    for (Int32 i = 0; i < count; i++)
                    {
                        var id = new ObjectId(answer.Packet.ReadBytes(12));
                        PlayerMapper.Add(id, playerservice);
                    }
                }
                catch (TimeoutException) { continue; }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="username"></param>
        public static void EraListener_OnConnected(MongoDB.Bson.ObjectId nodeId, String username)
        {
            PlayerSubscriptions.AddSubscriptionList(nodeId.ToString());

            // TODO: send all friends that are online a status update for this player
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="username"></param>
        public static void EraListener_OnDisconnected(MongoDB.Bson.ObjectId nodeId, String username)
        {
            // TODO: player logged off message
            var packet = EraListener.Server.CreateMessage(32);
            PlayerSubscriptions.PushPacket(nodeId.ToString(), packet);
            PlayerSubscriptions.RemoveSubscriptionList(nodeId.ToString());
        }
    }
}
