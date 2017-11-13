using System;
using System.Collections.Generic;
using Mono.Web;
using Newtonsoft.Json;

namespace TvHeadendIntegration.TvHeadend.Web
{
    public class TvhFormBuilder
    {
        private readonly List<string> _data = new List<string>();

        public static TvhFormBuilder Create()
        {
            return new TvhFormBuilder();
        }

        public TvhFormBuilder Add(string name, string value)
        {
            _data.Add($"{name}={HttpUtility.UrlEncode(value)}");
            return this;
        }

        public TvhFormBuilder AddJson(string name, object data)
        {
            var value = JsonConvert.SerializeObject(data, Formatting.None);
            return Add(name, value);
        }

        public override string ToString()
        {
            return string.Join("&", _data);
        }
    }
}