using SharedComponents.Settings;

namespace NewCamd
{
    public class Settings : IniSettings
    {
        protected override string Name => "NewCamd";
        public string Username { get; private set; } = "user";
        public string Password { get; private set; } = "pass";
        public int Port { get; private set; } = 15050;
        public string DesKey { get; private set; } = "0102030405060708091011121314";
        public int MaxWaitTimeInMs { get; private set; } = 6000; //1 minute

        public void Update()
        {
            Save();
        }
    }
}