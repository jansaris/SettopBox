namespace ChannelList { 
    public class IptvInfo
    {
        public string Url { get; set; }
        public int? Number { get; set; }
        public string Provider { get; set; }
        public string Name { get; set; }

        internal bool Complete()
        {
            return !string.IsNullOrWhiteSpace(Name) && Number.HasValue;
        }
    }
}
