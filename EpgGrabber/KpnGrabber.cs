using System;
using System.Collections.Generic;
using System.Linq;
using EpgGrabber.Models;
using EpgGrabber.Models.Kpn;
using log4net;

namespace EpgGrabber
{
    public class KpnGrabber : IGrabber
    {
        readonly ILog _logger;
        readonly KpnDownloader _downloader;
        readonly XmlTv _xmlTv;
        readonly ChannelList _channelList;
        

        public KpnGrabber(ILog logger, KpnDownloader downloader, XmlTv xmlTv, ChannelList channelList)
        {
            _logger = logger;
            _downloader = downloader;
            _xmlTv = xmlTv;
            _channelList = channelList;
        }

        public string Download(Func<bool> stopProcessing)
        {
            var pipedEpg = _downloader.DownloadEpgData(stopProcessing);
            if (stopProcessing()) return string.Empty;
            var channelsWithEpg = ConvertToChannels(pipedEpg, stopProcessing);
            if (stopProcessing()) return string.Empty;
            _channelList.LoadChannelsFromDisk();
            channelsWithEpg = _channelList.FilterOnSelectedChannels(channelsWithEpg);
            if (stopProcessing()) return string.Empty;
            return GenerateXmlTv(channelsWithEpg);
        }

        private List<Channel> ConvertToChannels(string pipedEpg, Func<bool> stopProcessing)
        {
            pipedEpg = StripXml(pipedEpg);
            var programs = new List<Models.Kpn.Program>();
            var schedules = new List<Models.Kpn.Schedule>();
            var program = false;
            var schedule = false;
            foreach (var line in pipedEpg.Split(new []{ '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (stopProcessing()) return null;

                var trimmed = line.Trim();
                if (trimmed.Equals("PROGRAM", StringComparison.InvariantCultureIgnoreCase))
                {
                    program = true;
                    schedule = false;
                }
                else if (trimmed.Equals("SCHEDULE", StringComparison.InvariantCultureIgnoreCase))
                {
                    program = false;
                    schedule = true;
                }
                else if (program)
                {
                    programs.Add(new Models.Kpn.Program(trimmed, _logger));
                }
                else if (schedule)
                {
                    schedules.Add(new Schedule(trimmed, _logger));
                }
            }

            return ConvertKpnDataToChannels(programs, schedules);
        }

        private List<Channel> ConvertKpnDataToChannels(List<Models.Kpn.Program> programs, List<Schedule> schedules)
        {
            var channels = new List<Channel>();
            foreach (var schedule in schedules)
            {
                try
                {
                    var matchingProgram = programs.FirstOrDefault(p => p.Id == schedule.ProgramId);
                    if (matchingProgram == null)
                    {
                        _logger.Warn($"Missing program for schedule with program id {schedule.ProgramId}");
                        continue;
                    }
                    var channel = channels.FirstOrDefault(c => c.Name == schedule.ChannelId);
                    if (channel == null)
                    {
                        _logger.Info($"Create channel {schedule.ChannelId}");
                        channel = new Channel() {Name = schedule.ChannelId, Programs = new List<Models.Program>()};
                        channels.Add(channel);
                    }
                    var program = new Models.Program();
                    program.Name = matchingProgram.ProgramName;
                    program.Description = matchingProgram.Description;
                    program.Id = matchingProgram.Id;
                    var end = long.Parse(schedule.StartTime) + int.Parse(schedule.Duration) * 60;
                    program.SetStart(schedule.StartTime);
                    program.SetEnd(end.ToString());
                    program.Genres = new List<Genre>();
                    if (!string.IsNullOrWhiteSpace(matchingProgram.Genre1))
                    {
                        program.Genres.Add(new Genre {Name = matchingProgram.Genre1, Language = "NL"});
                    }
                    if (!string.IsNullOrWhiteSpace(matchingProgram.Genre2))
                    {
                        program.Genres.Add(new Genre {Name = matchingProgram.Genre2, Language = "NL"});
                    }
                    channel.Programs.Add(program);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to convert schedule item with ID {schedule.Id} and ProgramId {schedule.ProgramId}: {ex.Message}");
                }
            }
            _logger.Info($"Converted data into {channels.Count} channels with {channels.Sum(c => c.Programs.Count)} programs");
            return channels;
        }

        private string StripXml(string pipedEpg)
        {
            const string startString = "<![CDATA[";
            const string endString = "]]>";
            var start = pipedEpg.IndexOf(startString, StringComparison.Ordinal);
            var end = pipedEpg.IndexOf(endString, StringComparison.Ordinal);
            if (start < 0 || end < 0) throw new EpgGrabberException($"Failed to find start ({start}) or end ({end}) point when stripping XML");
            start += startString.Length;
            return pipedEpg.Substring(start, end - start).Trim();
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
