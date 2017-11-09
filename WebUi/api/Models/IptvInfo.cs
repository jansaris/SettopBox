using System;

namespace WebUi.api.Models
{
    public class IptvInfo
    {
        public string Url { get; set; }
        public int? Number { get; set; }
        public string Provider { get; set; }
        public string Name { get; set; }
        public int KBps { get; set; }
        public int MBps { get; internal set; }
        public bool Keyblock { get; set; }
    }
}
