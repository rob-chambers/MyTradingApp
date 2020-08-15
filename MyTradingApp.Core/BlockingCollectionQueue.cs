using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MyTradingApp.Core
{
    /// <summary>
    /// Represents a thread-safe queue for processing actions.  Uses its own processing thread.
    /// </summary>
    internal class BlockingCollectionQueue : IQueueProcessor
    {
        private readonly BlockingCollection<Action> _jobs = new BlockingCollection<Action>();

        public BlockingCollectionQueue()
        {
            var thread = new Thread(new ThreadStart(OnStart))
            {
                IsBackground = true
            };
            thread.Start();
        }

        public void Enqueue(Action job)
        {
            _jobs.Add(job);
        }

        private void OnStart()
        {
            foreach (var job in _jobs.GetConsumingEnumerable(CancellationToken.None))
            {
                try
                {
                    job.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error("An error occurred invoking a job on the queue processor.\n{0}", ex);
                    throw;
                }
            }
        }
    }
}
