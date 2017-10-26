using System.Collections.Generic;
using System.Linq;

namespace SharedComponents.Models
{
    public class ChannelInfo
    {
        private List<ChannelLocation> _locations;

        public ChannelInfo()
        {
            Icons = new List<string>();
            _locations = new List<ChannelLocation>();
            Number = -1;
        }

        public string Key { get; set; }
        public string Name { get; set; }

        public IReadOnlyList<ChannelLocation> Locations => _locations.AsReadOnly();

        public List<string> Icons { get; set; }
        public bool Radio { get; set; }
        public int Number { get; set; }

        

        public string FirstLocationUrl => Locations.Any() ? Locations.First().Url : null;

        public int FirstLocationQuality => Locations.Any() ? Locations.First().Bitrate : 0;

        public void AddLocation(ChannelLocation location)
        {
            _locations.Add(location);
            _locations = _locations.OrderByDescending(l => l.Bitrate).ToList();
        }

        public override string ToString()
        {
            return $"{Name} ({Key}). {Locations?.Count ?? 0} locations";
        }
    }

}
