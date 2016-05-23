using System;
using System.Collections.Generic;

namespace EpgGrabber.Models
{
    public class EpgChannel
    {
        public string Channel { get; set; }
        public List<EpgProgram> Programs { get; set; }

        public override string ToString()
        {
            return $"{Channel}. {Programs?.Count ?? 0} progs";
        }
    }

    public class EpgProgram
    {
        private const string XmlTvDateFormat = "yyyyMMddHHmmss"; //yyyyMMddHHmmss zzz";

        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string OtherData { get; set; }
        public string Description { get; set; }
        public List<EpgGenre> Genres { get; set; }
        //TODO: more info to read from Glashart EPG

        /// <summary>
        /// Gets the start date in XMLTV format
        /// </summary>
        public string StartString => Start.ToString(XmlTvDateFormat);

        /// <summary>
        /// Gets the end date in XMLTV format
        /// </summary>
        public string EndString => End.ToString(XmlTvDateFormat);

        /// <summary>
        /// Sets the start date based on the start EPG string
        /// </summary>
        /// <param name="value">The value.</param>
        public void SetStart(string value)
        {
            Start = new DateTime(1970, 1, 1).AddSeconds(double.Parse(value));
        }
        /// <summary>
        /// Sets the end date based on the start EPG string
        /// </summary>
        /// <param name="value">The value.</param>
        public void SetEnd(string value)
        {
            End = new DateTime(1970, 1, 1).AddSeconds(double.Parse(value));
        }

        public override string ToString()
        {
            return $"{Start:dd-MM-yy HH:mm} / {End:dd-MM-yy HH:mm}  {Name}";
        }
    }

    public class EpgGenre
    {
        public string Language { get; set; }
        public string Genre { get; set; }
    }

    public class EpgDetails
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
