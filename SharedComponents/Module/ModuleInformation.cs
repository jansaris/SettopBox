using System.Collections.Generic;
using System.Linq;
using log4net;

namespace SharedComponents.Module
{
    public class ModuleInformation
    {
        readonly ILog _logger = LogManager.GetLogger(typeof (ModuleInformation));
        readonly List<IModule> _modules = new List<IModule>();

        public IEnumerable<string> Modules => _modules
            .Select(m => m.Name)
            .ToList();

        public void Register(IModule module)
        {
            _logger.Info($"Start monitoring {module.Name}");
            _modules.Add(module);
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
    }
}