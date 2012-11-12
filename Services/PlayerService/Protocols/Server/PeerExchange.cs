using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ERAUtils;
using ERAUtils.Logger;
using System.Net;
using Lidgren.Network;
using ProjectERA.Protocols;

namespace ERAServer.Protocols.Server
{
    internal class PeerExchange : Protocol
    {
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
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public PeerExchange(Connection connection)
            : base(connection)
        {
            lock (Instances)
            {
                foreach (Protocol p in Instances)
                {
                    if (p == this)
                        continue;

                    Logger.Debug("Suggesting " + this.Connection.NetConnection.RemoteEndpoint.ToString() + " to " + p.Connection.NetConnection.RemoteEndpoint.ToString());

                    // this.Connection equals argument equals newest connection
                    // p.Connection equals older connections
                    // Send older the newer endpoint

                    // Size = action byte + address length byte + max 4 address bytes + 2 port bytes
                    NetOutgoingMessage msg1 = p.Connection.MakeMessage(this.ProtocolIdentifier, 1 + 7);
                    msg1.Write((Byte)PeerExchangeAction.Introduce);
                    msg1.Write(this.Connection.NetConnection.RemoteEndpoint);
                    p.QueueAction(() =>

                        p.Connection.SendMessage(msg1, NetDeliveryMethod.ReliableUnordered));

                    // Send newer the older endpoint
                    NetOutgoingMessage msg2 = this.Connection.MakeMessage(this.ProtocolIdentifier, 1 + 7);
                    msg2.Write((Byte)PeerExchangeAction.Introduce);
                    msg2.Write(p.Connection.NetConnection.RemoteEndpoint);
                    this.QueueAction(() =>

                        this.Connection.SendMessage(msg2, NetDeliveryMethod.ReliableUnordered));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override Byte ProtocolIdentifier
        {
            get { return (Byte)ServerProtocols.PeerExchange; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        internal override void IncomingMessage(Lidgren.Network.NetIncomingMessage msg)
        {
            PeerExchangeAction action = (PeerExchangeAction)msg.ReadByte();
            switch (action)
            {
                case PeerExchangeAction.Introduce:
                    IPEndPoint p = msg.ReadIPEndpoint();
                    Logger.Info("Connecting to introduced server: " + p.ToString());

                    NetOutgoingMessage hail = this.Connection.NetManager.CreateMessage();
                    hail.Write(ERAUtils.Environment.LongMachineId);
                    hail.Write(Properties.Settings.Default.ServerName);

                    this.Connection.NetManager.Connect(p, hail);
                    /*this.Connection.NetManager.ConnectSRP(p,
                        ERAServer.Properties.Settings.Default.SRP6Keysize,
                        ERAServer.Properties.Settings.Default.ServerName,
                        ERAServer.Properties.Settings.Default.Secret, new Byte[] { 31 });
                    break;*/
                    break;
                default:
                    throw new NetException("No such action in this protocol " + this.GetType());
            }
        }
    }
}
