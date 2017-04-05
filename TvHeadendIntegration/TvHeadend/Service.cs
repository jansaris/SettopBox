using System;
using System.Collections.Generic;
using TvHeadendIntegration.TvHeadend.Web;
using log4net;

namespace TvHeadendIntegration.TvHeadend
{
    public class Service : TvhObject
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Service));

        /*TvHeadend properties*/
        public string multiplex_uuid { get; set; }
        public int? sid { get; set; }
        public string svcname { get; set; }
        public int? dvb_servicetype { get; set; }
        public int? created { get; set; }
        public int? last_seen { get; set; }
        public bool? enabled { get; set; }
        public string provider { get; set; }
        public int? pmt { get; set; }
        public int? pcr { get; set; }
        public List<Stream> stream { get; set; }

        public Service(int sid)
        {
            this.sid = sid;
            dvb_servicetype = 0;
            enabled = true;
            created = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            last_seen = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            stream = new List<Stream>();
        }

        /// <summary>
        /// Don't use this constructor
        /// This constructor will be used by Newtonsoft.Json
        /// </summary>
        public Service() : this(1)
        {

        }

        public override Urls Urls
        {
            get
            {
                return new Urls
                {
                    List = "/api/mpegts/service/grid",
                    Create = ""
                };
            }
        }
    }
}