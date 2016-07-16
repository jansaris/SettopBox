using System;
using System.Net;

namespace EpgGrabber.IO
{
    public class EpgWebClient : WebClient
    {
        readonly int _timeout;

        public EpgWebClient(Settings settings)
        {
            _timeout = settings.WebRequestTimeoutInMs;
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            var w = base.GetWebRequest(uri);
            w.Timeout = _timeout; //Allow max 5 seconds
            return w;
        }
    }
}