using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using EpgGrabber.IO;
using EpgGrabber.Models;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EpgGrabber
{
    public class Grabber
    {
        readonly ILog _logger;
        readonly Settings _settings;
        readonly IDownloader _downloader;
        readonly Compression _compression;
        readonly XmlTv _xmlTv;
        readonly IGenreTranslator _genreTranslator;

        public Grabber(ILog logger, Settings settings, IDownloader downloader, Compression compression, XmlTv xmlTv, IGenreTranslator genreTranslator)
        {
            _logger = logger;
            _settings = settings;
            _downloader = downloader;
            _compression = compression;
            _xmlTv = xmlTv;
            _genreTranslator = genreTranslator;
        }

        public void Download()
        {
            _logger.Info($"Start grabbing EPG for {_settings.NumberOfEpgDays} days");
            var date = DateTime.Today;
            var epg = new List<Channel>();

            //Download EPG for a couple of days
            for (var dayNr = 0; dayNr < _settings.NumberOfEpgDays; dayNr++)
            {
                //EPG is downloaded in 8 parts per day
                for (var dayPart = 0; dayPart < 8; dayPart++)
                {
                    _logger.Info($"Download EPG data for day {dayNr} part {dayPart}");
                    var zip = DownloadEpgfile(date, dayNr, dayPart);
                    var file = _compression.Decompress(zip);
                    var epgString = Encoding.Default.GetString(file);
                    var epgData = ParseEpgData(epgString);
                    DownloadDetails(epgData);
                    _logger.Info($"Downloaded EPG data for {epgData.SelectMany(channel => channel.Programs).Count()} programs");

                    epg.AddRange(epgData);
                }
            }

            //Order all programs
            foreach (var channel in epg)
            {
                channel.Programs = channel.Programs.OrderBy(c => c.Start).ToList();
            }

            GenerateXmlTv(epg);
        }

        byte[] DownloadEpgfile(DateTime now, int dayNr, int dayPart)
        {
            //EPG url example: http://w.zt6.nl/epgdata/epgdata.20141128.1.json.gz
            var url = $"epgdata.{now.AddDays(dayNr):yyyyMMdd}.{dayPart}.json.gz";
            url = string.Concat(_settings.EpgUrl, url);

            //Download the file
            try
            {
                return _downloader.DownloadBinary(url);
            }
            catch (Exception err)
            {
                _logger.Error($"Unable to download EPG for URL '{url}'", err);
                return null;
            }
        }

        /// <summary>
        /// Generates the XMLTV file
        /// </summary>
        void GenerateXmlTv(List<Channel> epg)
        {
            try
            {
                //Generate XMLTV file
                _xmlTv.GenerateXmlTv(epg);
                _logger.Info("XMLTV file generated");
            }
            catch (Exception err)
            {
                _logger.Error(err);
            }
        }

        void DownloadDetails(List<Channel> epg)
        {
            var programs = epg.SelectMany(channel => channel.Programs).ToList();
            //Loop over all the programs and try to load the details
            DownloadDetails(programs);
            TranslateProgramGenres(programs.Where(p => p.Genres != null));
        }

        void DownloadDetails(IReadOnlyCollection<Models.Program> list)
        {
            int failed = 0;
            foreach (var program in list)
            {
                //Download string
                _logger.DebugFormat("Try to download details for: {0}", program.Id);
                var details = DownloadDetails(program.Id);
                if (string.IsNullOrWhiteSpace(details))
                {
                    failed++;
                    continue;
                }
                //Parse and update
                var parsed = JsonConvert.DeserializeObject<Details>(details);
                program.Description = parsed.Description;
                if (parsed.Genres != null && parsed.Genres.Any())
                {
                    program.Genres = parsed.Genres.Select(g => new Genre { Name = g, Language = "nl" }).ToList();
                }
            }
            if(failed > 0) _logger.InfoFormat("Failed to load details for {0} programs", failed);
        }

        void TranslateProgramGenres(IEnumerable<Models.Program> programs)
        {
            foreach (var program in programs)
            {
                var newGenres = _genreTranslator.Translate(program.Genres);
                _logger.DebugFormat("Translated {0} genres for {1}", newGenres.Count, program.Id);
                program.Genres.AddRange(newGenres);
            }
        }

       List<Channel> ParseEpgData(string data)
        {
            List<Channel> result = new List<Channel>();

            var converter = new ExpandoObjectConverter();
            dynamic json = JsonConvert.DeserializeObject<ExpandoObject>(data, converter);
            if (json == null) return result;

            foreach (var channelName in json)
            {
                var channel = result.FirstOrDefault(c => c.Name.Equals((string)channelName.Key, StringComparison.InvariantCultureIgnoreCase));
                if (channel == null)
                {
                    channel = new Channel { Programs = new List<Models.Program>() };
                    result.Add(channel);
                    channel.Name = (string)channelName.Key;
                }

                //Add programms
                foreach (var program in channelName.Value)
                {
                    var prog = new Models.Program();

                    foreach (var programProperty in program)
                    {
                        string key = programProperty.Key?.ToString();
                        string value = programProperty.Value?.ToString();
                        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;

                        switch (key.ToLower())
                        {
                            case "id":
                                prog.Id = value;
                                break;
                            case "name":
                                prog.Name = value.DecodeNonAsciiCharacters();
                                break;
                            case "start":
                                prog.SetStart(value);
                                break;
                            case "end":
                                prog.SetEnd(value);
                                break;
                            case "disablerestart":
                                //Ignore
                                break;
                            default:
                                //I'm curious which other data is present
                                prog.OtherData = $"{prog.OtherData}{key}={value};";
                                break;
                        }
                    }

                    //Add the program when it does not exist yet
                    if (!channel.Programs.Any(p => p.Start == prog.Start && p.End == prog.End))
                        channel.Programs.Add(prog);
                }
            }

            return result;
        }

        string DownloadDetails(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length < 2)
            {
                _logger.Debug("No valid ID to download details");
                return null;
            }
            try
            {
                var dir = id.Substring(id.Length - 2, 2);
                var url = $"{_settings.EpgUrl}{dir}/{id}.json";
                _logger.Debug($"Try to download {url}");
                var data = _downloader.DownloadString(url);
                _logger.Debug($"Downloaded details: {data}");
                return data;
            }
            catch (Exception)
            {
                _logger.Debug("No detailed data found for id {id}");
                return null;
            }
        }
    }
}