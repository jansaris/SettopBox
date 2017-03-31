using System.Collections.Generic;
using System.IO;
using System.Linq;
using TvHeadendIntegration.TvHeadend.Web;
using log4net;
using Newtonsoft.Json;

namespace TvHeadendIntegration.TvHeadend
{
    public class Network : TvhObject
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Network));

        [JsonIgnore]
        public List<Mux> Muxes { get; set; }

        /*TvHeadend properties*/
        public int? priority { get; set; }
        public int? spriority { get; set; }
        public int? max_streams { get; set; }
        public int? max_bandwidth { get; set; }
        public int? max_timeout { get; set; }
        public string networkname { get; set; }
        public int? nid { get; set; }
        public bool? autodiscovery { get; set; }
        public bool? skipinitscan { get; set; }
        public bool? idlescan { get; set; }
        public bool? sid_chnum { get; set; }
        public bool? ignore_chnum { get; set; }
        public int? satip_source { get; set; }
        public bool? localtime { get; set; }

        public Network()
        {
            Muxes = new List<Mux>();

            priority = 1;
            spriority = 1;
            max_streams = 0;
            max_bandwidth = 0;
            max_timeout = 15;
            nid = 0;
            autodiscovery = true;
            skipinitscan = true;
            idlescan = false;
            sid_chnum = false;
            ignore_chnum = false;
            satip_source = 0;
            localtime = false;
        }

        public override Urls Urls
        {
            get
            {
                return new Urls
                {
                    List = "/api/mpegts/network/grid",
                    Create = "/api/mpegts/network/create"
                };
            }
        }
        protected override string ExtractId(string filename)
        {
            var folder = Path.GetDirectoryName(filename);
            return folder != null
                ? folder.Split(Path.DirectorySeparatorChar).Last()
                : base.ExtractId(filename);
        }
    }
}
