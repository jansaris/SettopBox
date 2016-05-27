using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;

namespace SharedComponents.Module
{
    public class ModuleCommunication
    {
        readonly ILog _logger = LogManager.GetLogger(typeof (ModuleCommunication));
        readonly List<IModule> _modules = new List<IModule>();
        public event EventHandler Update;

        public IEnumerable<string> Modules => _modules
            .Select(m => m.Name)
            .ToList();

        public void Register(IModule module)
        {
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
                Task.Run(() =>
                {
                    module.ProcessDataFromOtherModule(senderName, newData);
                });
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
            return mod?.GetModuleInfo() ?? null;
        }

        IModule Get(string name)
        {
            return _modules.FirstOrDefault(m => m.Name == name);
        }

        void OnUpdate()
        {
            //Inform the users in a new thread
            Task.Run(() =>
            {
                Update?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}