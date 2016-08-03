using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SharedComponents.Helpers;

namespace SharedComponents.Module
{
    public abstract class BaseModule : IModule
    {
        readonly LinuxSignal _signal;
        readonly ModuleCommunication _moduleCommunication;
        public string Name => GetType().Namespace;

        public ModuleState State { get; private set; }

        public abstract IModuleInfo GetModuleInfo();

        public event EventHandler<ModuleState> StatusChanged;
        public event EventHandler<CommunicationData> NewDataAvailable;

        protected const TaskStatus AsyncTaskIsRunning = TaskStatus.WaitingForActivation;

        protected BaseModule(LinuxSignal signal, ModuleCommunication moduleCommunication)
        {
            _signal = signal;
            _moduleCommunication = moduleCommunication;
        }

        bool _disposing;

        public void Start()
        {
            Directory.SetCurrentDirectory(AssemblyDirectory);
            if (State == ModuleState.Disabled) return;
            ChangeState(ModuleState.Starting);
            StartModule();
            _moduleCommunication.Register(this);
            _signal.Exit += StopModule;
            ChangeState(ModuleState.Running);
        }

        void StopModule(object sender, EventArgs e)
        {
            Stop();
        }

        public void Disable()
        {
            ChangeState(ModuleState.Disabled);
        }

        public void Stop()
        {
            if (State == ModuleState.Disabled) return;
            if (State == ModuleState.Stopping) return;
            if (State == ModuleState.Stopped) return;

            ChangeState(ModuleState.Stopping);
            _signal.Exit -= StopModule;
            _moduleCommunication.UnRegister(this);
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

        static string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}