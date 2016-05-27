using System;
using System.Threading.Tasks;

namespace SharedComponents.Module
{
    public abstract class BaseModule : IModule
    {
        public string Name => GetType().Namespace;

        public ModuleState State { get; private set; }

        public abstract IModuleInfo GetModuleInfo();

        public event EventHandler<ModuleState> StatusChanged;
        public event EventHandler<CommunicationData> NewDataAvailable;

        protected const TaskStatus AsyncTaskIsRunning = TaskStatus.WaitingForActivation;

        bool _disposing;

        public void Start()
        {
            if (State == ModuleState.Disabled) return;
            ChangeState(ModuleState.Starting);
            StartModule();
            ChangeState(ModuleState.Running);
        }

        public void Disable()
        {
            ChangeState(ModuleState.Disabled);
        }

        public void Stop()
        {
            if (State == ModuleState.Disabled) return;
            ChangeState(ModuleState.Stopping);
            StopModule();
            ChangeState(ModuleState.Stopped);
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
            ChangeState(ModuleState.Error);
        }

        protected void SignalNewData(DataType dataType, object data)
        {
            NewDataAvailable?.Invoke(this, new CommunicationData(dataType, data));
        }

        public virtual void ProcessDataFromOtherModule(string moduleName, CommunicationData data)
        {
            
        }

        void ChangeState(ModuleState newState)
        {
            State = newState;
            StatusChanged?.Invoke(this, newState);
        }
    }
}