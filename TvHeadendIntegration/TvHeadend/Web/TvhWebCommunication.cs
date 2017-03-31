using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mono.Web;

namespace TvHeadendIntegration.TvHeadend.Web
{
    public class TvhWebCommunication
    {
        private string _hostUrl = "192.168.10.20:9981";
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TvhWebCommunication));

        public void Create(string uri, object data)
        {
            var client = CreateWebClient();
            var uploadData = ConvertToQueryString(data);
            var url = string.Concat("http://",_hostUrl, uri);
            var result = client.UploadString(url, "POST", uploadData);
            Logger.Debug($"Created object on {url} with result {result}");
        }

        private string ConvertToQueryString(object data)
        {
            var json = TvhJsonConvert.Serialize(data);

            var jObj = (JObject)JsonConvert.DeserializeObject(json);
            var query = String.Join("&",
                jObj.Children().Cast<JProperty>()
                    .Select(jp => jp.Name + "=" + HttpUtility.UrlEncode(jp.Value.ToString())));
            return query;
        }

        private WebClient CreateWebClient()
        {
            var webClient = new WebClient();
            
            webClient.Headers.Add("Host", _hostUrl);
            //webClient.Headers.Add("Proxy-Connection", "keep-alive");
            webClient.Headers.Add("Origin", _hostUrl);
            webClient.Headers.Add("X-Requested-With", "XMLHttpRequest");
            webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.130 Safari/537.36");
            webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            //webClient.Headers.Add("Accept", "*/*");
            webClient.Headers.Add("referer", string.Concat("http://", _hostUrl, "/extjs.html"));
            webClient.Headers.Add("Accept-Encoding", "gzip, deflate");
            webClient.Headers.Add("Accept-Language", "nl-NL,nl;q=0.8,en-US;q=0.6,en;q=0.4");
            return webClient;

            /*
                Host: _host
                Proxy-Connection: keep-alive
                Content-Length: 464
                Origin: _host
                X-Requested-With: XMLHttpRequest
                User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.130 Safari/537.36
                Content-Type: application/x-www-form-urlencoded; charset=UTF-8
                Accept: \*\/\*
                Referer: http:// _host /extjs.html
                Accept-Encoding: gzip, deflate
                Accept-Language: nl-NL,nl;q=0.8,en-US;q=0.6,en;q=0.4
             */
        }

        public void WaitUntilScanCompleted()
        {
            while (!IsScanComplete())
            {
                Logger.InfoFormat("Wait 1 second until scan is complete");
                Task.Delay(1000).Wait();
            }
        }

        private bool IsScanComplete()
        {
            var client = CreateWebClient();
            var url = string.Concat("http://", _hostUrl, "/api/mpegts/mux/grid");
            var data = client.DownloadString(url);
            var deserialized = JsonConvert.DeserializeObject<TvhTable<Mux>>(data);
            return deserialized.entries.All(s => s.scan_state == 0);
        }
    }
}