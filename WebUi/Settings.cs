using SharedComponents.Settings;

namespace WebUi
{
    public class Settings : IniSettings
    {
        protected override string Name => "WebUi";
        public int Port { get; private set; } = 15051;
    }
}