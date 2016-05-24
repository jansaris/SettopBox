using SharedComponents.Settings;

namespace EpgGrabber
{
    public class Settings : IniSettings
    {
        protected override string Name => "EpgGrabber";
        public int Hour { get; set; } = 21;
        public int Minute { get; set; } = 0;
        public string EpgUrl { get; set; } = "http://w.zt6.nl/epgdata/";
        public int NumberOfEpgDays { get; set; } = 7;
        public string XmlTvFileName { get; set; } = "Epg.xml";
        public string DataFolder { get; set; } = "Data";
        public string EpgTranslationsFile { get; set; } = "EpgGenres.txt";
    }
}