using System;
using System.Collections.Generic;

namespace EpgGrabber.Models
{
    public class Program
    {
        private const string XmlTvDateFormat = "yyyyMMddHHmmss"; //yyyyMMddHHmmss zzz";

        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string OtherData { get; set; }
        public string Description { get; set; }
        public List<Genre> Genres { get; set; }
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
}