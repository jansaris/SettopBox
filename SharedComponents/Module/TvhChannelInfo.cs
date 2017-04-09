using System;

namespace SharedComponents.Module
{
    public class TvHeadendChannelInfo : ICloneable
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string UUID { get; set; }

        public object Clone()
        {
            return new TvHeadendChannelInfo
            {
                Name = Name,
                Url = Url,
                UUID = UUID
            };
        }
    }
}