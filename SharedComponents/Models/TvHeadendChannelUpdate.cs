namespace SharedComponents.Models
{
    public class TvHeadendChannelUpdate
    {
        public string Id { get; set; }
        public string OldUrl { get; set; }
        public string NewUrl { get; set; }
        public bool Epg { get; set; }
    }
}
