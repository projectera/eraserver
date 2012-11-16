using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Lidgren.Network;
using ERA.Protocols.ClientProtocol;
using MongoDB.Bson;

namespace ERA.Tools.HeadlessClient.Protocols
{
    internal partial class Player : Protocol
    {
        /// <summary>
        /// Pickavatar action
        /// </summary>
        /// <param name="selectedId">avatar to select</param>
        internal static void RequestPickAvatar(ObjectId selectedId, Action<ObjectId> resultAction)
        {
            // Mark map not loaded
            //Map.Id = ObjectId.Empty;
            _pickAvatarAction = resultAction;

            // Send message
            NetOutgoingMessage msg = OutgoingMessage(PlayerAction.PickAvatar, 12);
            msg.Write(selectedId.ToByteArray());
            _connection.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        internal static void RequestMovement(Int32 x, Int32 y, Byte d)
        {
            NetOutgoingMessage msg = OutgoingMessage(ERA.Protocols.ClientProtocol.PlayerAction.RequestMovement, 9);
            msg.Write(x);
            msg.Write(y);
            msg.Write(d);
            _connection.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
        }

        /// <summary>
        /// PlayerRequest class
        /// </summary>
        private class PlayerRequest : ICacheable<ObjectId>
        {
            /// <summary>
            /// Player Id
            /// </summary>
            public ObjectId Key { get; set; }

            /// <summary>
            /// Task that will yield Player object
            /// </summary>
            internal TaskCompletionSource<ERA.Protocols.PlayerProtocol.Player> Task { get; private set; }

            /// <summary>
            /// Action that will run upon completion
            /// </summary>
            internal Action<ERA.Protocols.PlayerProtocol.Player> Action { get; private set; }

            /// <summary>
            /// Retrieval Timer
            /// </summary>
            internal Timer TimeOut { get; set; }

            /// <summary>
            /// Creation Time of Request
            /// </summary>
            internal DateTime Creation { get; private set; }

            /// <summary>
            /// Creates a new DataStoreRequest
            /// </summary>
            /// <param name="key">The key to request</param>
            public PlayerRequest(ObjectId key)
            {
                this.Key = key;
                this.Task = new TaskCompletionSource<ERA.Protocols.PlayerProtocol.Player>();
                this.Creation = DateTime.Now;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            /// <param name="action"></param>
            public PlayerRequest(ObjectId key, Action<ERA.Protocols.PlayerProtocol.Player> action)
                : this(key)
            {
                this.Action = action;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="player"></param>
            internal ERA.Protocols.PlayerProtocol.Player Result
            {
                set
                {
                    if (!this.Task.TrySetResult(value))
                    {

                    }

                    // Kill timout
                    if (this.TimeOut != null)
                        this.TimeOut.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

                    // Run action
                    if (this.Action != null)
                        this.Action.Invoke(value);

                    // Kill action
                    this.Action = null;
                }
                get
                {
                    return this.Task.Task.Result;
                }
            }
        }
    }
}
