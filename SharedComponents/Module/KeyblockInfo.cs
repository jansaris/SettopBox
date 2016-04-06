using System;

namespace SharedComponents.Module
{
    public class KeyblockInfo : IModuleInfo
    {
        public bool HasValidKeyblock { get; set; }
        public DateTime? LastRetrieval { get; set; }
        public DateTime? NextRetrieval { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }
}