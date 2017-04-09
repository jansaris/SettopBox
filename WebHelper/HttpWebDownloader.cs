using System;
using log4net;
using System.Net;

namespace WebHelper
{
    public class HttpWebDownloader : IWebDownloader
    {
        readonly Func<EpgWebClient> _webClientFactory;
        readonly ILog _logger;

        public HttpWebDownloader(Func<EpgWebClient> webClientFactory, ILog logger)
        {
            _webClientFactory = webClientFactory;
            _logger = logger;
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
                _logger.Error($"Failed to download the data from {url}", ex);
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
            catch (WebException ex)
            {
                _logger.Warn($"Failed to download the string from {url}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to download the string from {url}", ex);
                return null;
            }
        }
    }
}