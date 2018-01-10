using System.Linq;
using System.Text.RegularExpressions;
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
        public string DataFolder { get; set; } = "Data";
        public string RawChannelsFile { get; set; } = "ChannelList.raw";
        public string ChannelsFile { get; set; } = "ChannelList.txt";
        public bool ScanForKeyblockIds { get; set; } = false;
        public string MachineId { get; set; } = "";

        public string GetMacAddress()
        {
            if (string.IsNullOrWhiteSpace(MachineId)) return string.Empty;
            if (MachineId.Contains("-")) return MachineId.ToLowerInvariant();
            return string.Join("-", Regex.Matches(MachineId, @".{2}").Cast<Match>()).ToLowerInvariant();
        }
    }
}