using System;
using System.Collections.Generic;
using System.Linq;
using log4net;

namespace SharedComponents.Module
{
    public class ModuleCommunication
    {
        readonly IThreadHelper _threadHelper;
        readonly ILog _logger = LogManager.GetLogger(typeof (ModuleCommunication));
        readonly List<IModule> _modules = new List<IModule>();
        public event EventHandler Update;

        public IEnumerable<string> Modules => _modules
            .Select(m => m.Name)
            .ToList();

        public ModuleCommunication(IThreadHelper threadHelper)
        {
            _threadHelper = threadHelper;
        }

        public void Register(IModule module)
        {
            if (_modules.Contains(module))
            {
                _logger.Info($"Ignore registration of {module.Name} because it is already registered");
                return;
            }
            _logger.Info($"Start monitoring {module.Name}");
            module.NewDataAvailable += Module_NewDataAvailable;
            module.StatusChanged += Module_StatusChanged;
            _modules.Add(module);
        }

        void Module_StatusChanged(object sender, ModuleState newState)
        {
            OnUpdate();
        }

        void Module_NewDataAvailable(object sender, CommunicationData newData)
        {
            var senderName = (sender as IModule)?.Name ?? "Unknown";
            foreach (var module in _modules)
            {
                //Skip the sender
                if(module.Name == senderName) continue;
                //Inform all the modules, each in a own task

                _threadHelper.RunSafeInNewThread(() =>
                {
                    module.ProcessDataFromOtherModule(senderName, newData);
                }, _logger);
            }
            OnUpdate();
        }

        public void UnRegister(IModule module)
        {
            _modules.Remove(module);
        }

        public bool Enabled(string name)
        {
            var mod = Get(name);
            if (mod == null) return false;
            return !mod.State.HasFlag(ModuleState.Disabled);
        }

        public string Status(string name)
        {
            var mod = Get(name);
            return mod?.State.ToString() ?? "Unknown Module";
        }

        public IModuleInfo Info(string name)
        {
            var mod = Get(name);
            return mod?.GetModuleInfo();
        }

        public IModuleInfo Data(string name)
        {
            var mod = Get(name);
            return mod?.GetData();
        }

        IModule Get(string name)
        {
            return _modules.FirstOrDefault(m => m.Name == name);
        }

        public void Start(string name)
        {
            var mod = Get(name);
            _logger.Info($"Start module {mod?.Name} ({name})");
            mod?.Start();
        }

        public void Stop(string name)
        {
            var mod = Get(name);
            _logger.Info($"Stop module {mod?.Name} ({name})");
            mod?.Stop();
        }

        void OnUpdate()
        {
            //Inform the users in a new thread
            _threadHelper.RunSafeInNewThread(() =>
            {
                Update?.Invoke(this, EventArgs.Empty);
            },_logger);
        }
    }
}