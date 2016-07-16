using System;
using System.Net;
using log4net;

namespace EpgGrabber.IO
{
    public class EpgWebClient : WebClient
    {
        readonly ILog _logger;
        readonly int _timeout;

        public EpgWebClient(ILog logger, Settings settings)
        {
            _logger = logger;
            _timeout = settings.WebRequestTimeoutInMs;
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            _logger.Debug($"Generate request for {uri} with {_timeout}ms timeout");
            var w = base.GetWebRequest(uri);
            w.Timeout = _timeout; 
            return w;
        }
    }
}