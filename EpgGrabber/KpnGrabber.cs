using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using SharedComponents.Iptv;
using WebHelper;

namespace EpgGrabber
{
    public class KpnGrabber : IGrabber
    {
        readonly ILog _logger;
        readonly Settings _settings;
        readonly IDownloader _downloader;
        readonly XmlTv _xmlTv;
        readonly ChannelList _channelList;
        

        public KpnGrabber(ILog logger, Settings settings, IDownloader downloader, XmlTv xmlTv, ChannelList channelList)
        {
            _logger = logger;
            _settings = settings;
            _downloader = downloader;
            _xmlTv = xmlTv;
            _channelList = channelList;
        }

        public string Download(Func<bool> stopProcessing)
        {
            throw new NotImplementedException();
        }
    }
}
