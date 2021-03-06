﻿using System.Collections.Generic;
using System.Linq;

namespace SharedComponents.Models
{
    public class ChannelInfo
    {

        public ChannelInfo()
        {
            Icons = new List<string>();
            Locations = new List<ChannelLocation>();
            Names = new List<string>();
            Number = -1;
        }

        public string Key { get; set; }
        public string Name => Names.FirstOrDefault();

        public List<string> Names { get; set; }

        public List<ChannelLocation> Locations { get; set; }

        public List<string> Icons { get; set; }
        public bool Radio { get; set; }
        public int Number { get; set; }

        public int FirstLocationQuality => Locations.Any() ? Locations.First().Bitrate : 0;

        public void AddLocation(ChannelLocation location)
        {
            Locations.Add(location);
            Locations = Locations.OrderByDescending(l => l.Bitrate).ToList();
        }

        public override string ToString()
        {
            return $"{Name} ({Key}). {Locations?.Count ?? 0} locations";
        }

        public void AddName(string channelName)
        {
            if(!Names.Contains(channelName)) Names.Add(channelName);
        }
    }

}
