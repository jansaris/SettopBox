using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebUi.api.Models
{
    public class Channel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Tuple<string, string>> AvailableChannels { get; set; }
        public string TvHeadendChannel { get; set; }
        public int KeyblockId { get; set; }

        public bool Keyblock { get; set; }
        public bool EpgGrabber { get; set; }
        public bool TvHeadend { get; set; }
        public int Number { get; internal set; }
    }
}
