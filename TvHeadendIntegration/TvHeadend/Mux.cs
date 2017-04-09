using System.Collections.Generic;
using System.IO;
using System.Linq;
using TvHeadendIntegration.TvHeadend.Web;
using log4net;
using Newtonsoft.Json;

namespace TvHeadendIntegration.TvHeadend
{
    public class Mux : TvhObject
    {
        [JsonIgnore]
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Mux));

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

        public override object CreateData
        {
            get
            {
                return new
                {
                    uuid = network_uuid,
                    conf = this
                };
            }
        }

        public override object UpdateData
        {
            get
            {
                return this;
            }
        }

        [JsonIgnore]
        public List<Service> Services { get; set; }

        /*TvHeadend properties*/
        public string network_uuid { get; set; }
        public string iptv_url { get; set; }
        public string iptv_interface { get; set; }
        public string iptv_muxname { get; set; }
        public string iptv_sname { get; set; }
        public bool? iptv_atsc { get; set; }
        public bool? iptv_respawn { get; set; }
        public bool? enabled { get; set; }
        public int? epg { get; set; }
        public int? onid { get; set; }
        public int? tsid { get; set; }
        public int? scan_result { get; set; }
        public int? scan_state { get; set; }
        public int? pmt_06_ac3 { get; set; }
        
        public Mux()
        {
            Services = new List<Service>();

            iptv_atsc = false;
            iptv_respawn = false;
            enabled = true;
            epg = 1;
            onid = 0;
            tsid = 0;
            scan_result = 0;
            pmt_06_ac3 = 0;
        }

        public Service ResolveService(string name)
        {
            return Services.OrderBy(s => s.sid).FirstOrDefault(s => s.svcname.Contains(name));
        }
    }
}