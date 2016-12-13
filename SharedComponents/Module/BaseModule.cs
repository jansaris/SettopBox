using System;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net;
using SharedComponents.Helpers;

namespace SharedComponents.Module
{
    public abstract class BaseModule : IModule
    {
        protected ILog Logger { get; private set; }
        readonly LinuxSignal _signal;
        readonly ModuleCommunication _moduleCommunication;
        public string Name => GetType().Namespace;

        private readonly object _syncRoot = new object();
        private ModuleState _state = ModuleState.Initial;

        public ModuleState State
        {
            get
            {
                lock (_syncRoot)
                {
                    return _state;
                }
            }
            set
            {
                lock (_syncRoot)
                {
                    Logger.Info($"Change module state from {State} to {value}");
                    _state = value;
                }
            }
        }

        public abstract IModuleInfo GetModuleInfo();

        public event EventHandler<ModuleState> StatusChanged;
        public event EventHandler<CommunicationData> NewDataAvailable;

        protected BaseModule(ILog logger, LinuxSignal signal, ModuleCommunication moduleCommunication)
        {
            Logger = logger;
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
            StopModule();
            ChangeState(ModuleState.Stopped);
        }
        public bool ModuleShouldStop()
        {
            switch (State)
            {
                case ModuleState.Initial:
                case ModuleState.Starting:
                case ModuleState.Idle:
                case ModuleState.Running:
                    return false;
                default:
                    return true;
            }
        }

        protected void WaitForSpecificState(ModuleState state)
        {
            var counter = 0;
            while (State != state && !ModuleShouldStop())
            {
                if (counter > 60)
                {
                    counter = 0;
                    Logger.Debug($"WaitForSpecificState: Still waiting for state {state}");
                }
                //Wait for 1 second, and re-evaluate
                Thread.Sleep(1000);
                counter++;
            }
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
            _moduleCommunication.UnRegister(this);
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

        protected void ChangeState(ModuleState newState)
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