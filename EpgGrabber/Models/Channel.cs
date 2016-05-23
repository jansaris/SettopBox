using System;
using System.Collections.Generic;
using System.Linq;

namespace EpgGrabber.Models
{
    public class Channel
    {
        public Channel()
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

        public override string ToString()
        {
            return $"{Name}:{Url}";
        }
    }

    public class ChannelListItem
    {
        public int Number { get; set; }
        public string OriginalName { get; set; }
        public string NewName { get; set; }

        /// <summary>
        /// Gets the name for writing to M3U
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return (string.IsNullOrWhiteSpace(NewName) ? OriginalName : NewName);
        }

        public override string ToString()
        {
            return
                $"{Number}:{OriginalName}{(!string.IsNullOrWhiteSpace(NewName) ? string.Concat(" (", NewName, ")") : null)}";
        }

        /// <summary>
        /// Parses the specified text line.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <returns></returns>
        public static ChannelListItem ReadFromString(string textLine)
        {
            int number;
            var parts = textLine.Split(',');
            if (parts.Length != 2 && parts.Length != 3) return null;
            if (!int.TryParse(parts[0].Trim(), out number)) return null;

            return new ChannelListItem { Number = number, OriginalName = parts[1].Trim(), NewName = (parts.Length == 3 ? parts[2].Trim() : null) };
        }

        public string WriteToString()
        {
            return string.IsNullOrWhiteSpace(NewName)
                ? string.Join(",", Number, OriginalName)
                : string.Join(",", Number, OriginalName, NewName);
        }
    }

    public class M3UChannel
    {
        public string Number { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }

        public override string ToString()
        {
            return $"{Number}:{Name} - {Url}";
        }
    }
}