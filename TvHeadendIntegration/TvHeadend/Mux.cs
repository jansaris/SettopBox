using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TvHeadendIntegration.TvHeadend.Web;

namespace TvHeadendIntegration.TvHeadend
{
    public class Mux : TvhObject
    {
        public override string CreateUrl
        {
            get { return "/api/mpegts/network/mux_create"; }
        }

        public override Urls Urls
        {
            get
            {
                return new Urls
                {
                    List = "/api/mpegts/mux/grid",
                    Create = "/api/mpegts/network/mux_create"
                };
            }
        }

        public override string CreateData => TvhFormBuilder.Create()
            .Add("uuid", network_uuid)
            .AddJson("conf", new
            {
                enabled = 1,
                epg,
                iptv_url,
                iptv_atsc = false,
                iptv_muxname,
                channel_number,
                iptv_sname
            })
            .ToString();

        public override string UpdateData => TvhFormBuilder.Create().AddJson("node", this).ToString();

        [JsonIgnore]
        public List<Service> Services { get; set; }

        /*TvHeadend properties*/
        public string network_uuid { get; set; }
        public string iptv_url { get; set; }
        public string iptv_muxname { get; set; }
        public int channel_number { get; set; }
        public string iptv_sname { get; set; }
        public bool iptv_atsc { get; set; }
        public bool enabled { get; set; }
        public int epg { get; set; }
        public int scan_state { get; set; }

        public Mux()
        {
            Services = new List<Service>();

            iptv_atsc = false;
            enabled = true;
            epg = 1;
        }

        public Service ResolveService(string name)
        {
            return Services.OrderBy(s => s.sid).FirstOrDefault(s => s.svcname.Contains(name));
        }
    }
}