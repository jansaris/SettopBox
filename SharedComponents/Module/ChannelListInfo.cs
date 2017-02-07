using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedComponents.Module
{
    public class ChannelListInfo : ModuleInfo
    {
        public List<ChannelInfo> Channels { get; set; }
        public DateTime? LastRetrieval { get; set; }
        public string State { get; set; }
    }

    public class ChannelInfo
    {
        public ChannelInfo()
        {
            Icons = new List<string>();
            Locations = new List<ChannelLocation>();
            Number = -1;
        }

        public string Key { get; set; }
        public string Name { get; set; }
        public List<ChannelLocation> Locations { get; set; }
        public List<string> Icons { get; set; }
        public bool Radio { get; set; }
        public int Number { get; set; }

        public void OrderLocations(List<string> importanceList)
        {
            if (importanceList == null || !importanceList.Any()) return;
            //Get the list ordered based on the importance list
            var newList = importanceList
                .Select(item => Locations.FirstOrDefault(l => item.Equals(l.Name, StringComparison.InvariantCultureIgnoreCase)))
                .Where(location => location != null)
                .ToList();
            //Add the missing ones which where not in the importance list to the end of the list
            newList.AddRange(Locations.Where(l => !newList.Contains(l)));
            //Set the new list
            Locations = newList;
        }

        public string FirstLocationUrl
        {
            get
            {
                if (Locations == null) Locations = new List<ChannelLocation>();
                return Locations.Any() ? Locations.First().Url : null;
            }
        }

        public string FirstLocationQuality
        {
            get
            {
                if (Locations == null) Locations = new List<ChannelLocation>();
                return Locations.Any() ? Locations.First().Name : null;
            }
        }

        public override string ToString()
        {
            return $"{Name} ({Key}). {Locations?.Count ?? 0} locations";
        }
    }
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