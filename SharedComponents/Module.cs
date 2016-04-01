using System;
using System.Threading.Tasks;

namespace SharedComponents
{
    public abstract class BaseModule : IModule
    {
        public enum Status
        {
            Disabled,
            Starting,
            Running,
            Stopping,
            Stopped,
            Error
        };

        public string Name => GetType().Namespace;

        public Status State { get; private set; }

        public EventHandler<Status> StatusChanged;

        bool _disposing;

        protected const TaskStatus AsyncTaskIsRunning = TaskStatus.WaitingForActivation;

        public void Start()
        {
            if (State == Status.Disabled) return;
            ChangeState(Status.Starting);
            StartModule();
            ChangeState(Status.Running);
        }

        public void Disable()
        {
            ChangeState(Status.Disabled);
        }

        public void Stop()
        {
            if (State == Status.Disabled) return;
            ChangeState(Status.Stopping);
            StopModule();
            ChangeState(Status.Stopped);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected abstract void StartModule();
        protected abstract void StopModule();
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _disposing) return;
            _disposing = true;
            Stop();
        }

        protected void Error()
        {
            ChangeState(Status.Error);
        }

        void ChangeState(Status newState)
        {
            State = newState;
            StatusChanged?.Invoke(this, newState);
        }
    }
}