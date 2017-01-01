using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Layout;
using SharedComponents.Module;
using WebHelper;

namespace ChannelList
{
    public class ChannelList
    {
        private readonly ILog _logger;
        private readonly Settings _settings;
        private readonly IWebDownloader _downloader;
        private readonly Compression _compression;
        private readonly JavascriptParser _javascriptParser;

        public ChannelList(ILog logger, Settings settings, IWebDownloader downloader, Compression compression, JavascriptParser javascriptParser)
        {
            _logger = logger;
            _settings = settings;
            _downloader = downloader;
            _compression = compression;
            _javascriptParser = javascriptParser;
        }

        public List<ChannelInfo> Load()
        {
            try
            {
                _logger.Info($"Start downloading data from {_settings.Url}");
                var compressed = _downloader.DownloadBinary(_settings.Url);
                var uncompressed = _compression.Decompress(compressed);
                var javascript = System.Text.Encoding.UTF8.GetString(uncompressed);
                _logger.Info($"Received {javascript.Length} characters of javascript");
                var channels = _javascriptParser.ParseChannnels(javascript);
                return channels.Where(c => c.Number != -1).OrderBy(c => c.Number).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load the channel list: {ex.Message}", ex);
                return null;
            }
        }
    }
}
