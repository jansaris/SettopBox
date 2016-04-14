using SharedComponents.Settings;

namespace WebUi
{
    public class Settings : IniSettings
    {
        protected override string Name => "WebUi";
        public int Port { get; private set; } = 15051;
        public string Host { get; private set; } = "localhost";
        public string WwwRootFolder { get; private set; } = "./www";
    }
}