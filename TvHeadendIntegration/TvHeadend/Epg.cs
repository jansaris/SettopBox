using System.Collections.Generic;
using TvHeadendIntegration.TvHeadend.Web;
using log4net;

namespace TvHeadendIntegration.TvHeadend
{
    public class Epg : TvhObject
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Epg));

        /*TvHeadend properties*/
        public string name { get; set; }
        public List<string> channels { get; set; }

        public Epg()
        {
            name = string.Empty;
            channels = new List<string>();
        }

        public void AddChannel(Channel channel)
        {
            if (channel == null) return;
            if (channels.Contains(channel.uuid)) return;

            channels.Add(channel.uuid);
        }

        public void RemoveChannel(Channel channel)
        {
            if (channel == null) return;
            channels.Remove(channel.uuid);
        }

        public override Urls Urls
        {
            get
            {
                return new Urls
                {
                    Create = string.Empty,
                    List = "/api/epggrab/channel/list",
                };
            }
        }
    }
}