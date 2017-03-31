using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TvHeadendIntegration.TvHeadend
{
    public class Stream
    {
        public int pid { get; set; }
        public string type { get; set; }
        public int position { get; set; }
        public int? audio_type { get; set; }

        /*Tvheadend extra properties*/
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        public static Stream CreateVerimatrix()
        {
            var stream = new Stream
            {
                pid = 102,
                type = "CA",
                position = 262144,
                _additionalData = new Dictionary<string, JToken> {{"caidlist", JArray.Parse("[{ \"caid\": 22017 }]")}}
            };
            return stream;
        }

        public static Stream CreateH264()
        {
            return new Stream
            {
                pid = 237,
                type = "H264",
                position = 0
            };
        }

        public static Stream CreateTeletext()
        {
            return new Stream
            {
                pid = 273,
                type = "TELETEXT",
                position = 0
            };
        }

        public static Stream CreateAc3()
        {
            return new Stream
            {
                pid = 2237,
                type = "AC3",
                position = 0,
                audio_type = 0,
                _additionalData = new Dictionary<string, JToken> {{"language", JValue.CreateString("dut")}}
            };
        }

        public bool IsVerimatrixStream()
        {
            if (pid != 102) return false;
            if (type != "CA") return false;
            if (position != 262144) return false;
            if (!_additionalData.ContainsKey("caidlist")) return false;
            return true;
        }
    }
}