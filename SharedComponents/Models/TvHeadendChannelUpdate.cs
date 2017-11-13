namespace SharedComponents.Models
{
    public class TvHeadendChannelUpdate
    {
        public string TvhId { get; set; }
        public int Number { get; set; }
        public string NewUrl { get; set; }
        public string Name { get; set; }
        public bool Epg { get; set; }
    }
}
