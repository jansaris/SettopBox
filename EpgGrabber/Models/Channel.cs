using System.Collections.Generic;

namespace EpgGrabber.Models
{
    public class Channel
    {
        public string Name { get; set; }
        public List<Program> Programs { get; set; }

        public override string ToString()
        {
            return $"{Name}. {Programs?.Count ?? 0} progs";
        }
    }
}
