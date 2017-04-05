using System;
using System.Linq;
using System.Net;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mono.Web;

namespace TvHeadendIntegration.TvHeadend.Web
{
    public class TvhCommunication
    {
        private readonly string _hostAddress;
        private readonly string _username;
        private readonly string _password;
        private readonly ILog _logger;
        private const int ListCount = 50;

        public TvhCommunication(ILog logger, Settings settings)
        {
            _logger = logger;
            _hostAddress = settings.WebUrl ?? string.Empty;
            if (!_hostAddress.StartsWith("http://")) _hostAddress = string.Concat("http://", _hostAddress);
            _username = settings.Username;
            _password = settings.Password;
        }

        public TvhTable<T> GetTableResult<T>(string uri)
        {
            var current = 0;
            var table = GetTableResult<T>(uri, current, ListCount);
            if (table == null) return new TvhTable<T>();
            current += ListCount;
            while (current < table.total)
            {
                var newTable = GetTableResult<T>(uri, current, ListCount);
                table.entries.AddRange(newTable.entries);
                current += ListCount;
            }
            return table;
        }

        private TvhTable<T> GetTableResult<T>(string uri, int start, int count)
        {
            var query = string.Format("?start={0}&limit={1}", start, count);
            var sResult = GetData(string.Concat(uri,query));
            if (sResult == null) return default(TvhTable<T>);
            try
            {
                return JsonConvert.DeserializeObject<TvhTable<T>>(sResult,new JsonSerializerSettings{});
            }
            catch (Exception)
            {
                _logger.Error($"Failed to convert the response into a table of {typeof(T).Name}. Result: {sResult}");
                return default(TvhTable<T>);
            }
        }

        internal bool TestAuthentication()
        {
            try
            {
                using (var client = CreateWebClient())
                {
                    _logger.Debug($"Test authentication on {_hostAddress}");
                    var url = string.Concat(_hostAddress, "/extjs.html");
                    var result = client.DownloadString(url);
                    _logger.Info($"Succesfully authenticated on {_hostAddress}");
                    return true;
                }
            }
            catch(WebException ex) when((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.Warn($"Authentication denied for user {_username} on {_hostAddress}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to to test communication with tvheadend ({_hostAddress})", ex);
                return false;
            }
        }

        public T Post<T>(string uri, object data)
        {
            var sResult = PostData(uri, data);
            if (sResult == null) return default(T);
            try
            {
                return JsonConvert.DeserializeObject<T>(sResult);
            }
            catch (Exception)
            {
                _logger.Error($"Failed to convert the response {typeof(T).Name}. Result: {sResult}");
                return default(T);
            }
        }

        public void Post(string uri, object data)
        {
            PostData(uri, data);
        }

        private string PostData(string uri, object data)
        {
            try
            {
                using (var client = CreateWebClient())
                {
                    var uploadData = ConvertToQueryString(data);
                    var url = string.Concat(_hostAddress, uri);
                    var result = client.UploadString(url, "POST", uploadData);
                    _logger.Debug($"Posted object on {url} with result {result}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to post data to tvheadend ({_hostAddress}) at {uri}", ex);
                return null;
            }
        }

        private string GetData(string uri)
        {
            try
            {
                using (var client = CreateWebClient())
                {
                    var url = string.Concat(_hostAddress, uri);
                    var result = client.DownloadString(url);
                    _logger.Debug($"Get from {url} with result {result}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get data from tvheadend ({_hostAddress}) at {uri}", ex);
                return null;
            }
        }

        private WebClient CreateWebClient()
        {
            var webClient = new WebClient();
            var host = _hostAddress.Substring(7);
            webClient.Headers.Add("Host", host);
            webClient.Headers.Add("Origin", host);
            webClient.Headers.Add("X-Requested-With", "XMLHttpRequest");
            webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.130 Safari/537.36");
            webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            webClient.Headers.Add("referer", string.Concat("http://", _hostAddress, "/extjs.html"));
            webClient.Headers.Add("Accept-Encoding", "identity");
            webClient.Headers.Add("Accept-Language", "nl-NL,nl;q=0.8,en-US;q=0.6,en;q=0.4");
            if(!string.IsNullOrWhiteSpace(_username))
            {
                NetworkCredential myCreds = new NetworkCredential(_username, _password);
                webClient.Credentials = myCreds;
            }
            
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

        private static string ConvertToQueryString(object data)
        {
            var json = TvhJsonConvert.Serialize(data);

            var jObj = (JObject)JsonConvert.DeserializeObject(json);
            var query = String.Join("&",
                jObj.Children().Cast<JProperty>()
                    .Select(jp => jp.Name + "=" + HttpUtility.UrlEncode(jp.Value.ToString())));
            return query;
        }
    }
}