using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Lidgren.Network;
using PlayerService.Connections;

namespace PlayerService.Protocols
{
    abstract class Protocol : IDisposable
    {
        /// <summary>
        /// The list of Instances of this Protocol
        /// </summary>
        public virtual List<Protocol> Instances
        {
            get;
            set;
        }

        /// <summary>
        /// The connection this instance belongs to
        /// </summary>
        public ClientConnection Connection { get; private set; }

        /// <summary>
        /// The id of this protocol
        /// </summary>
        public abstract byte ProtocolIdentifier { get; }

        /// <summary>
        /// 
        /// </summary>
        public CancellationTokenSource ErrorCancelation { get; private set; }

        /// <summary>
        /// This holds the last task that is going to be executed.
        /// </summary>
        private Task _endTask;
        private Task _lastTask;
        private Object _taskLock = new Object();

        /// <summary>
        /// Initializes protocol
        /// </summary>
        private void Initialize()
        {
            if (this.Instances == null)
                this.Instances = new List<Protocol>();
        }

        /// <summary>
        /// Runs a function on all instances
        /// </summary>
        /// <param name="action">The action to be performed</param>
        protected static void Broadcast(Protocol protocolType, Action<Protocol, ClientConnection> action)
        {   
            foreach (Protocol p in protocolType.Instances)
                p.QueueAction(() => action.Invoke(p, p.Connection));
        }

        /// <summary>
        /// This creates a new Protocol
        /// </summary>
        /// <param name="connection">The connection this protocol belongs to</param>
        public Protocol(ClientConnection connection)
        {
            this.Connection = connection;
            this.ErrorCancelation = new CancellationTokenSource();

            Initialize();

            lock (this.Instances)
                Instances.Add(this);
        }

        /// <summary>
        /// This function handles incoming messages for this Protocol
        /// </summary>
        /// <param name="msg">The received message</param>
        internal abstract void IncomingMessage(NetIncomingMessage msg);

        /// <summary>
        /// This function is run when the client disconnects
        /// </summary>
        internal virtual void Disconnect()
        {

        }

        /// <summary>
        /// This removes this Protocol instance from the instance list
        /// </summary>
        internal void DeRegister()
        {
            lock (this.Instances)
                this.Instances.Remove(this);
        }

        /// <summary>
        /// This queues a new action to be run on this instance
        /// </summary>
        /// <param name="action">The action to be run</param>
        internal Task QueueAction(Action action)
        {
            lock (_taskLock)
            {
                if (_endTask == null)
                {
                    if (ErrorCancelation.IsCancellationRequested)
                        return Task.Factory.StartNew(() => /*Logger.Notice("Action queued on error'd protocol instance")*/ { });
                    _endTask = Task.Factory.StartNew(action, ErrorCancelation.Token);
                }
                else
                    _endTask = _lastTask.ContinueWith((Task t) =>
                    {
                        if (t.Exception != null)
                        {
                            this.Connection.Error(t.Exception);

                            // Wait until we are canceled
                            SpinWait.SpinUntil(() => this.ErrorCancelation.IsCancellationRequested);
                            this.ErrorCancelation.Token.ThrowIfCancellationRequested();
                        }
                        action.Invoke();
                    }, this.ErrorCancelation.Token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);

                // After the last task (endTask) is run, the actionQueue is updated (lastTask)
                _lastTask = _endTask.ContinueWith(UpdateActionQueue);
                return _endTask;
            }   
        }

        /// <summary>
        /// Finalizes endtask
        /// </summary>
        /// <param name="t">The task that just finished</param>
        protected void UpdateActionQueue(Task t)
        {
            lock(_taskLock)
            {
                if (_endTask == t)
                    _endTask = null;
            }

            if (t.IsFaulted)
                this.Connection.Error(t.Exception);
        }

        /// <summary>
        /// Disposes this protocol
        /// </summary>
        public virtual void Dispose()
        {

        }
    }
}
