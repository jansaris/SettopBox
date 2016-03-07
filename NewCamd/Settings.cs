using SharedComponents.Settings;

namespace NewCamd
{
    public class Settings : IniSettings
    {
        protected override string Name => "NewCamd";
        public string Username { get; private set; } = "user";
        public string Password { get; private set; } = "pass";
        public int Port { get; private set; } = 15050;
        public string DesKey { get; private set; } = "";

        public void Update()
        {
            Save();
        }
    }
}