using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
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
            _logger.Info("Start grabbing EPG");
            DownloadEpgfiles();
            DecompressEpgFiles();
            var epg = ReadEpgfiles();
            epg = DownloadDetails(epg);
            GenerateXmlTv(epg);

        }

        /// <summary>
        /// Generates the XMLTV file
        /// </summary>
        public void GenerateXmlTv(List<EpgChannel> epg)
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

        public List<EpgChannel> DownloadDetails(List<EpgChannel> epg)
        {
            var programs = epg.SelectMany(channel => channel.Programs).ToList();
            //Loop over all the programs and try to load the details
            DownloadDetails(programs);
            TranslateProgramGenres(programs.Where(p => p.Genres != null));

            return epg;
        }

        private void DownloadDetails(IReadOnlyCollection<EpgProgram> list)
        {
            int succes = 0, failed = 0, percent = 0;
            foreach (var program in list)
            {
                //Log percentage of completion
                var current = (int)(((succes + failed) / (float)list.Count) * 100);
                if (current != percent)
                {
                    percent = current;
                    _logger.InfoFormat("Downloading {0}% EPG program details", percent);
                }
                //Download string
                _logger.DebugFormat("Try to download details for: {0}", program.Id);
                var details = DownloadDetails(program.Id);
                if (string.IsNullOrWhiteSpace(details))
                {
                    failed++;
                    continue;
                }
                //Parse and update
                var parsed = JsonConvert.DeserializeObject<EpgDetails>(details);
                program.Description = parsed.Description;
                if (parsed.Genres != null && parsed.Genres.Any())
                {
                    program.Genres = parsed.Genres.Select(g => new EpgGenre { Genre = g, Language = "nl" }).ToList();
                }
                _logger.DebugFormat("Updated program {0}", program.Id);
                succes++;
            }

            _logger.InfoFormat("Succesfully loaded details for {0} programs", succes);
            _logger.InfoFormat("Failed to load details for {0} programs", failed);
        }

        void TranslateProgramGenres(IEnumerable<EpgProgram> programs)
        {
            foreach (var program in programs)
            {
                var newGenres = _genreTranslator.Translate(program.Genres);
                _logger.DebugFormat("Translated {0} genres for {1}", newGenres.Count, program.Id);
                program.Genres.AddRange(newGenres);
            }
        }

        void DecompressEpgFiles()
        {
            foreach (var file in Directory.GetFiles(_settings.EpgFolder, "*.gz"))
            {
                _logger.Debug($"Decompress {file}");
                _compression.Decompress(file, file.Substring(0, file.Length - 3));
            }
        }

        /// <summary>
        /// Reads the EPG files into a list of EPG objects
        /// </summary>
        List<EpgChannel> ReadEpgfiles()
        {
            var result = new List<EpgChannel>();

            var date = DateTime.Today;
            for (var dayNr = 0; dayNr < _settings.NumberOfEpgDays; dayNr++)
            {
                //EPG is in 8 parts
                for (var dayPart = 0; dayPart < 8; dayPart++)
                {
                    var name = $"epgdata.{date.AddDays(dayNr):yyyyMMdd}.{dayPart}.json";
                    var uncompressedFile = Path.Combine(_settings.EpgFolder, name);

                    //Read the JSON file
                    try
                    {
                        if (File.Exists(uncompressedFile))
                        {
                            var converter = new ExpandoObjectConverter();
                            dynamic json = JsonConvert.DeserializeObject<ExpandoObject>(File.ReadAllText(uncompressedFile), converter);
                            if (json == null) continue;
                            
                            foreach (var channelName in json)
                            {
                                var channel = result.FirstOrDefault(c => c.Channel.Equals((string)channelName.Key, StringComparison.InvariantCultureIgnoreCase));
                                if (channel == null)
                                {
                                    channel = new EpgChannel { Programs = new List<EpgProgram>() };
                                    result.Add(channel);
                                    channel.Channel = (string)channelName.Key;
                                }

                                //Add programms
                                foreach (var program in channelName.Value)
                                {
                                    var prog = new EpgProgram();

                                    foreach (var programProperty in program)
                                    {
                                        string key = programProperty.Key?.ToString();
                                        string value = programProperty?.Value?.ToString();
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
                            
                        }
                        else
                        {
                            _logger.Debug($"EPG file {name} not found to read");
                        }
                    }
                    catch (Exception err)
                    {
                        _logger.Error($"Unable to decompress EPG file '{name}'", err);
                    }
                }
            }

            //Order all programs
            foreach (var channel in result)
                channel.Programs = channel.Programs.OrderBy(c => c.Start).ToList();

            return result;
        }

        /// <summary>
        /// Downloads the EPG (compressed) files.
        /// </summary>
        void DownloadEpgfiles()
        {
            //EPG url example: http://w.zt6.nl/epgdata/epgdata.20141128.1.json.gz
            var date = DateTime.Today;
            for (var dayNr = 0; dayNr < _settings.NumberOfEpgDays; dayNr++)
            {
                //EPG is downloaded in 8 parts
                for (var dayPart = 0; dayPart < 8; dayPart++)
                {

                    var url = $"epgdata.{date.AddDays(dayNr):yyyyMMdd}.{dayPart}.json.gz";
                    var localFile = Path.Combine(_settings.EpgFolder, url);
                    url = string.Concat(_settings.EpgUrl, url);

                    //Download the file
                    try
                    {
                        _downloader.DownloadBinaryFile(url, localFile);
                    }
                    catch (Exception err)
                    {
                        _logger.Error($"Unable to download EPG for URL '{url}'", err);
                    }
                }
            }
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
                //var data = "{\"id\":\"061079be-1516-4a4d-ad50-ba394557b6ad\",\"name\":\"NOS Journaal / Actueel / herhalingen NOS Journaal / Extra onderwerpen\",\"start\":1431075600,\"end\":1431097200,\"description\":\"Het nieuws van de dag.\",\"genres\":[\"Actualiteit\",\"Info\"],\"disableRestart\":false}";
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