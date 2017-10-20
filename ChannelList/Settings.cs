using log4net;
using SharedComponents.Settings;

namespace ChannelList
{
    public class Settings : IniSettings
    {
        public Settings(ILog logger) : base(logger)
        {
        }
        protected override string Name => "ChannelList";
        public string Host { get; set; } = "213.75.116.138";
        public int Port { get; set; } = 8554;
    }
}