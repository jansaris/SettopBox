using SharedComponents.Settings;

namespace SettopBox
{
    public class Settings : IniSettings
    {
        protected override string Name => "SettopBox";
        public bool NewCamdEnabled { get; private set; } = true;
        public bool KeyblockEnabled { get; private set; } = true;
    }
}