using System;
using System.Collections.Generic;
using System.Linq;
using EpgGrabber.Models;
using log4net;
using Newtonsoft.Json;
using WebHelper;

namespace EpgGrabber
{
    public class ItvOnlineGrabber : IGrabber
    {
        readonly ILog _logger;
        readonly Settings _settings;
        readonly IDownloader _downloader;
        readonly XmlTv _xmlTv;
        readonly ChannelList _channelList;

        Func<bool> _stop;

        public ItvOnlineGrabber(ILog logger, Settings settings, IDownloader downloader, XmlTv xmlTv, ChannelList channelList)
        {
            _logger = logger;
            _settings = settings;
            _downloader = downloader;
            _xmlTv = xmlTv;
            _channelList = channelList;
        }

        public string Download(Func<bool> stopProcessing)
        {
            _logger.Info($"Start grabbing EPG for {_settings.NumberOfEpgDays} days");
            _stop = stopProcessing;
            var date = DateTime.Today;
            var epg = new List<Channel>();
            _channelList.LoadChannelsFromDisk();

            //Download EPG for a couple of days
            for (var dayNr = 0; dayNr < _settings.NumberOfEpgDays; dayNr++)
            {
                //EPG is downloaded in 8 parts per day
                for (var dayPart = 0; dayPart < 8; dayPart++)
                {
                    if (_stop()) return null;
                    var start = date.AddDays(dayNr).AddHours(3 * dayPart);
                    var end = date.AddDays(dayNr).AddHours(3 + 3 * dayPart);
                    var epgData = DownloadPart(start, end);
                    if(epgData!=null) epg.AddRange(epgData);
                }
            }

            //Order all programs
            foreach (var channel in epg)
            {
                channel.Programs = channel.Programs.OrderBy(c => c.Start).ToList();
            }

            return GenerateXmlTv(epg);
        }

        List<Channel> DownloadPart(DateTime start, DateTime end)
        {
            var epgData = new List<Channel>();
            try
            {
                _logger.Debug($"Download EPG data for {start:s} - {end:s}");
                var epgString = DownloadEpgJson(start, end);
                if (_stop()) return null;
                var jsonData = JsonConvert.DeserializeObject<ItvOnlineJson>(epgString);
                var data = ConvertToChannelList(jsonData);
                epgData = _channelList.FilterOnSelectedChannels(data);
                _logger.Info($"Downloaded EPG data for {epgData.SelectMany(channel => channel.Programs).Count()} programs for {start:s} - {end:s}");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed during download for {start:s} - {end:s}", ex);
                var inner = ex.InnerException;
                while (inner != null)
                {
                    _logger.Warn($"Caused by: {inner.GetType().Name} - {inner.Message}");
                    inner = inner.InnerException;
                }

            }
            return epgData;
        }

        private List<Channel> ConvertToChannelList(ItvOnlineJson jsonData)
        {
            return jsonData.resultObj.channelList.Select(c => new Channel
            {
                Name = c.channelName,
                Programs = c.programList.Select(p => new Models.Program
                {
                    Name = p.title,
                    Description = p.contentDescription,
                    Id = p.contentId.ToString(),
                    OtherData  = p.subtitle,
                    Start = FromEpoch(p.startTime),
                    End = FromEpoch(p.endTime)
                }).ToList()
            }).ToList();
        }

        private DateTime FromEpoch(int epochTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochTime).ToLocalTime();
        }

        string DownloadEpgJson(DateTime start, DateTime end)
        {
            //EPG url example: https://www.itvonline.nl/AVS/besc?action=GetEpg&channel=IPAD&endTimeStamp=1508854352&startTimeStamp=1508852352
            var url = string.Format(_settings.EpgUrl, AsEpoch(end), AsEpoch(start));

            //Download the file
            try
            {
                _logger.Debug($"Download {url}");
                return _downloader.DownloadString(url);
            }
            catch (Exception err)
            {
                _logger.Error($"Unable to download EPG for URL '{url}'", err);
                return null;
            }
        }

        private int AsEpoch(DateTime date)
        {
            var t = date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (int)t.TotalSeconds;
        }

        /// <summary>
        /// Generates the XMLTV file
        /// </summary>
        string GenerateXmlTv(List<Channel> epg)
        {
            try
            {
                //Generate XMLTV file
                var file = _xmlTv.GenerateXmlTv(epg);
                _logger.Info("XMLTV file generated");
                return file;
            }
            catch (Exception err)
            {
                _logger.Error(err);
                return null;
            }
        }
    }
}