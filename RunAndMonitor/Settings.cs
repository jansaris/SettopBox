using log4net;
using SharedComponents.Settings;

namespace RunAndMonitor
{
    public class Settings : IniSettings
    {
        public Settings(ILog logger) : base(logger)
        {
        }
        protected override string Name => "RunAndMonitor";

        public string WorkingDirectory { get; private set; }
        public string Executable { get; private set; }
        public string Arguments { get; private set; }
    }
}