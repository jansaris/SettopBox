using System;

namespace WebUi.api.Models
{
    public class RtpInfo
    {
        public int Number { get; set; }
        public string Provider { get; set; }
        public string Name { get; set; }

        internal bool Complete()
        {
            return Number > 0 &&
                   !string.IsNullOrWhiteSpace(Provider) &&
                   !string.IsNullOrWhiteSpace(Name);
        }
    }
}
