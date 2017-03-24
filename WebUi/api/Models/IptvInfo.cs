namespace WebUi.api.Models
{
    public class IptvInfo
    {
        public string Url { get; set; }
        public int? Number { get; set; }
        public string Provider { get; set; }
        public string Name { get; set; }
        public int KBps { get; set; }
        public int MBps { get; internal set; }

        internal bool Complete()
        {
            return Number.HasValue &&
                   !string.IsNullOrWhiteSpace(Provider) &&
                   !string.IsNullOrWhiteSpace(Name);
        }
    }
}
