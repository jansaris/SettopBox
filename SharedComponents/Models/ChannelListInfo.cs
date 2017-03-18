using SharedComponents.Module;
using System;
using System.Collections.Generic;

namespace SharedComponents.Models
{
    public class ChannelListInfo : ModuleInfo
    {
        public List<ChannelInfo> Channels { get; set; }
        public DateTime? LastRetrieval { get; set; }
        public string State { get; set; }
    }


}
