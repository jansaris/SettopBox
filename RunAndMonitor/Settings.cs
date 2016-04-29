using SharedComponents.Settings;

namespace RunAndMonitor
{
    public class Settings : IniSettings
    {
        protected override string Name => "RunAndMonitor";

        public string WorkingDirectory { get; private set; }
        public string Executable { get; private set; }
        public string Arguments { get; private set; }
    }
}