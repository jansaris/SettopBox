using System;
using log4net;
using System.Net;
using System.Text;

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

        public byte[] DownloadBinary(string url, bool noCache = false)
        {
            try
            {
                var webClient = _webClientFactory();
                var data = webClient.DownloadData(url);
                _logger.Debug($"Downloaded {data.Length} bytes from {url}");
                return data;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to download the data from {url}", ex);
                return null;
            }
        }

        public string DownloadString(string url, bool noCache = false)
        {
            try
            {
                var webClient = _webClientFactory();
                var data = webClient.DownloadData(url);
                return Encoding.UTF8.GetString(data);
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