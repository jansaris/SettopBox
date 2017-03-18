namespace SharedComponents.Models
{
    public class ChannelLocation
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public bool RtpSkip { get; set; }

        public override string ToString()
        {
            return $"{Name}:{Url}";
        }
    }
}
