using System.Collections.Generic;
using System.Linq;
using log4net;

namespace SharedComponents
{
    public class ModuleInformation
    {
        readonly ILog _logger = LogManager.GetLogger(typeof (ModuleInformation));
        readonly List<IModule> _modules = new List<IModule>();

        public List<string> Modules => _modules
            .Select(m => m.Name)
            .ToList();

        public List<string> EnabledModules => _modules
            .Where(m => m.State != ModuleState.Disabled)
            .Select(m => m.Name)
            .ToList();

        public List<string> DisabledModules => _modules
            .Where(m => m.State == ModuleState.Disabled)
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
    }
}