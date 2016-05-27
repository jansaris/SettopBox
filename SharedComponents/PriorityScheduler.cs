using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharedComponents
{
    public class PriorityScheduler : TaskScheduler
    {
        public static readonly PriorityScheduler AboveNormal = new PriorityScheduler(ThreadPriority.AboveNormal);
        public static readonly PriorityScheduler BelowNormal = new PriorityScheduler(ThreadPriority.BelowNormal);
        public static readonly PriorityScheduler Lowest = new PriorityScheduler(ThreadPriority.Lowest);

        readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();
        Thread[] _threads;
        readonly ThreadPriority _priority;
        readonly int _maximumConcurrencyLevel = Math.Max(1, Environment.ProcessorCount);

        public PriorityScheduler(ThreadPriority priority)
        {
            _priority = priority;
        }

        public override int MaximumConcurrencyLevel => _maximumConcurrencyLevel;

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks;
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);

            if (_threads != null) return;

            _threads = new Thread[_maximumConcurrencyLevel];
            for (var i = 0; i < _threads.Length; i++)
            {
                var local = i;
                _threads[local] = new Thread(() =>
                {
                    foreach (var t in _tasks.GetConsumingEnumerable())
                        TryExecuteTask(t);
                })
                {
                    Name = $"PriorityScheduler: {local}",
                    Priority = _priority,
                    IsBackground = true
                };
                _threads[local].Start();
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false; // we might not want to execute task that should schedule as high or low priority inline
        }
    }
}