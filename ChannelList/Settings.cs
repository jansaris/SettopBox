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
        public string Url { get; private set; }
    }
}