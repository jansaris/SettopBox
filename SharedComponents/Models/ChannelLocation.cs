namespace SharedComponents.Models
{
    public class ChannelLocation
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Protocol { get; set; }
        public string Url => $"{Protocol}://{Host}:{Port}";
        public int Bitrate { get; set; }
        public int KeyblockId { get; set; } = -1;

        public override string ToString()
        {
            return $"{Url} -- {Bitrate} bps";
        }
    }
}
