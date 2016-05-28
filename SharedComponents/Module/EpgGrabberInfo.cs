using System;

namespace SharedComponents.Module
{
    public class EpgGrabberInfo : IModuleInfo
    {
        public DateTime? LastRetrieval { get; set; }
        public DateTime? NextRetrieval { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string[] Channels { get; set; }
    }
}