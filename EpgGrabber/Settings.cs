using log4net;
using SharedComponents.Settings;

namespace EpgGrabber
{
    public class Settings : IniSettings
    {
        public Settings(ILog logger) : base(logger)
        {
        }
        protected override string Name => "EpgGrabber";
        public int Hour { get; set; } = 21;
        public int Minute { get; set; } = 0;
        public string EpgUrl { get; set; } = "http://w.zt6.nl/epgdata/";
        public int NumberOfEpgDays { get; set; } = 7;
        public string XmlTvFileName { get; set; } = "Epg.xml";
        public string DataFolder { get; set; } = "Data";
        public string EpgTranslationsFile { get; set; } = "TvhEpgGenres.txt";
        public string EpgChannelListFile { get; set; } = "Channels.txt";
        public bool InitialEpgGrab { get; set; } = true;

        public int WebRequestTimeoutInMs { get; set; } = 5000;
    }
}