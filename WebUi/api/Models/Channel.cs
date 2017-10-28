using SharedComponents.Models;
using System.Collections.Generic;
using System.Linq;

namespace WebUi.api.Models
{
    public class Channel
    {
        public string TvhId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public List<ChannelLocation> AvailableChannels { get; set; }
        public string TvHeadendChannel { get; set; }
        public int KeyblockId { get; set; }
        public bool Keyblock => AvailableChannels.Any(c => c.Keyblock);
        public bool EpgGrabber { get; set; }
        public bool TvHeadend { get; set; }
        public int Number { get; set; }
    }
}
