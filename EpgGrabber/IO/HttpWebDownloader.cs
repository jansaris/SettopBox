using System;
using log4net;

namespace EpgGrabber.IO
{
    public class HttpWebDownloader : IWebDownloader
    {
        readonly Func<EpgWebClient> _webClientFactory;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CachedWebDownloader));

        public HttpWebDownloader(Func<EpgWebClient> webClientFactory)
        {
            _webClientFactory = webClientFactory;
        }

        public byte[] DownloadBinary(string url)
        {
            try
            {
                var webClient = _webClientFactory();
                return webClient.DownloadData(url);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to download the data from {url}", ex);
                return null;
            }
        }

        public string DownloadString(string url)
        {
            try
            {
                var webClient = _webClientFactory();
                return webClient.DownloadString(url);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to download the string from {url}", ex);
                return null;
            }
        }
    }
}