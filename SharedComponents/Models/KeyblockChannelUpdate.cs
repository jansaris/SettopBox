namespace SharedComponents.Models
{
    public class KeyblockChannelUpdate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public int OldKey { get; set; }
        public int NewKey { get; set; }
    }
}
