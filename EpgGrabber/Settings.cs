using SharedComponents.Settings;

namespace EpgGrabber
{
    public class Settings : IniSettings
    {
        protected override string Name => "EpgGrabber";
        public int Hour { get; set; } = 21;
        public int Minute { get; set; } = 0;
    }
}