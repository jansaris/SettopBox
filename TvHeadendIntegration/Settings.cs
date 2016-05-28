using log4net;
using SharedComponents.Settings;

namespace TvHeadendIntegration
{
    public class Settings : IniSettings
    {
        public Settings(ILog logger) : base(logger)
        {
        }
        protected override string Name => "TvHeadendIntegration";
        public string XmlTvSocket { get; set; } = "/volume1/@appstore/tvheadend-4.0/var/epggrab/xmltv.sock";
        public string DataFolder { get; set; } = "Data";
        public string InitialEpgFile { get; set; } = "Epg.xml";
    }
}