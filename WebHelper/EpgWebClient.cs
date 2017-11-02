using System;
using System.Net;
using log4net;

namespace WebHelper
{
    public class EpgWebClient : WebClient
    {
        readonly ILog _logger;
        readonly int _timeout;

        public EpgWebClient(ILog logger)
        {
            _logger = logger;
            _timeout = 60000;
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            _logger.Debug($"Generate request for {uri} with {_timeout}ms timeout");
            var w = base.GetWebRequest(uri);
            if (w != null)
            {
                w.Timeout = _timeout; 
            }
            return w;
        }
    }
}