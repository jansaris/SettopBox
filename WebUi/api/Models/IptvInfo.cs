namespace WebUi.api.Models
{
    public class IptvInfo
    {
        public string Url { get; set; }
        public int? Number { get; set; }
        public string Provider { get; set; }
        public string Name { get; set; }
        public int Kbs { get; set; }

        internal bool Complete()
        {
            return Number.HasValue &&
                   !string.IsNullOrWhiteSpace(Provider) &&
                   !string.IsNullOrWhiteSpace(Name);
        }
    }
}
