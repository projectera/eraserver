using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ERA.Services.Map
{
    public class ActionQueue
    {
        protected Object _taskLock;
        protected Task _endTask, _lastTask;

        /// <summary>
        /// 
        /// </summary>
        public CancellationTokenSource ErrorCancelation { get; protected set; }

        /// <summary>
        /// Creates a new action queue
        /// </summary>
        public ActionQueue()
        {
            _taskLock = new Object();
            this.ErrorCancelation = new CancellationTokenSource();
        }

        /// <summary>
        /// This queues a new action to be run on this instance
        /// </summary>
        /// <param name="action">The action to be run</param>
        public Task QueueAction(Action action)
        {
            lock (_taskLock)
            {
                if (_endTask == null)
                {
                    // ErrorCancellation occured
                    if (ErrorCancelation.IsCancellationRequested)
                        return Task.Factory.StartNew(() => Console.WriteLine("Action queued on errored actionqueue instance"));

                    // Set the end task
                    _endTask = Task.Factory.StartNew(action, ErrorCancelation.Token);
                }
                else
                {
                    _endTask = _lastTask.ContinueWith((Task t) =>
                        {
                            if (t.Exception != null)
                            {
                                this.ErrorCancelation.Cancel();

                                // Wait until we are canceled
                                SpinWait.SpinUntil(() => this.ErrorCancelation.IsCancellationRequested);
                                this.ErrorCancelation.Token.ThrowIfCancellationRequested();
                            }
                            action.Invoke();
                        }, this.ErrorCancelation.Token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);

                }

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
            lock (_taskLock)
            {
                if (_endTask == t)
                    _endTask = null;
            }

            if (t.IsFaulted)
                this.ErrorCancelation.Cancel();
        }
    }
}
