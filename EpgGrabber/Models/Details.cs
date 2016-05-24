using System.Collections.Generic;

namespace EpgGrabber.Models
{
    public class Details
    {
        //{"id":"838882ca-79c4-409f-9966-2f9121c94f0e","name":"Lara","start":1430757300,"end":1430760300,"description":"","genres":["Serie"],"disableRestart":false}
        public string Id { get; set; }
        public string Name { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string Description { get; set; }
        public List<string> Genres { get; set; }
        public bool DisableRestart { get; set; }
    }
}