using TvHeadendIntegration.TvHeadend.Web;
using log4net;

namespace TvHeadendIntegration.TvHeadend
{
    public class Tag : TvhObject
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Tag));

        /*TvHeadend properties*/
        public bool enabled { get; set; }
        public int index { get; set; }
        public string name { get; set; }
        public bool @internal { get; set; }
        public bool @private { get; set; }
        public string icon { get; set; }
        public bool titled_icon { get; set; }
        public string comment { get; set; }

        public Tag()
        {
            enabled = true;
            index = -1;
            name = string.Empty;
            @internal = false;
            @private = false;
            icon = string.Empty;
            titled_icon = false;
            comment = string.Empty;
        }

        public override Urls Urls
        {
            get
            {
                return new Urls
                {
                    List = "/api/channeltag/grid",
                    Create = "/api/channeltag/create"
                };
            }
        }
    }
}