using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using SharedComponents.Iptv;
using System.IO.Compression;

namespace EpgGrabber
{
    public class KpnDownloader
    {
        readonly Settings _settings;
        readonly ILog _logger;
        readonly Func<IptvSocket> _socketFactory;

        public KpnDownloader(Settings settings, Func<IptvSocket> socketFactory, ILog logger)
        {
            _settings = settings;
            _socketFactory = socketFactory;
            _logger = logger;
        }

        public string DownloadEpgData(Func<bool> stopProcessing)
        {
            var data = DownloadRawData(stopProcessing);
            if (stopProcessing()) return string.Empty;
            var unzipped = UnzipData(data);
            if (stopProcessing()) return string.Empty;
            var epgData = Encoding.ASCII.GetString(unzipped);
            var file = Path.Combine(_settings.DataFolder, "rawepgdata.txt");
            _logger.Debug($"Write unzipped EPG data to disk: {file}");
            File.WriteAllText(file, epgData);
            return epgData;
        }

        private byte[] DownloadRawData(Func<bool> stopProcessing)
        {
            var rawFile = Path.Combine(_settings.DataFolder, "rawepgdata.gzip");
            if (stopProcessing()) return null;
            if (File.Exists(rawFile))
            {
                _logger.Debug($"Use data from disk: {rawFile}");
                return File.ReadAllBytes(rawFile);
            }

            var rawData = RetrieveDataFromCarousel(stopProcessing);

            _logger.Debug($"Write raw data to disk: {rawFile}");
            File.WriteAllBytes(rawFile, rawData);

            return rawData;
        }

        private byte[] RetrieveDataFromCarousel(Func<bool> stopProcessing)
        {
            try
            {
                var dictionary = new Dictionary<int, byte[]>();
                var data = new List<byte>();

                using (var socket = _socketFactory())
                {
                    socket.Open(GetHost(), GetPort());
                    var complete = false;
                    _logger.Info("Start collecting data from the EPG carousel");

                    var count = 1;
                    while (!complete)
                    {
                        if (stopProcessing()) return null;

                        var packet = socket.Receive();
                        var id = BitConverter.ToInt32(packet.Skip(4).Take(4).Reverse().ToArray(), 0);
                        if (!dictionary.ContainsKey(id))
                        {
                            dictionary.Add(id, packet.Skip(8).ToArray());
                        }
                        else
                        {
                            complete = ValidateIfWeHaveAllPackages(dictionary);
                        }
                        if(++count % 50 == 0) _logger.Debug($"Received {count} packtes");
                        if(count > 10240) throw new EpgGrabberException($"It took to many cycles ({count}) to collect all the data from the EPG carousel");
                    }

                    _logger.Info($"Collected {dictionary.Count} packets from the EPG carousel");
                    for (var i = dictionary.Keys.Min(); i <= dictionary.Keys.Max(); i++)
                    {
                        data.AddRange(dictionary[i]);
                    }
                }

                return data.ToArray();
            }
            catch (Exception ex)
            {
                throw new EpgGrabberException($"Failed to download the data from the EPG carousel: {ex.Message}", ex);
            }
        }

        private bool ValidateIfWeHaveAllPackages(Dictionary<int, byte[]> dictionary)
        {
            for (var i = dictionary.Keys.Min(); i < dictionary.Keys.Max(); i++)
            {
                if (dictionary.ContainsKey(i)) continue;

                _logger.Debug($"We are missing key {i}");
                return false;
            }
            return true;
        }

        private string GetHost()
        {
            return _settings.EpgUrl.Split(':')[0];
        }

        private int GetPort()
        {
            try
            {
                return int.Parse(_settings.EpgUrl.Split(':')[1]);
            }
            catch (Exception ex)
            {
                throw new EpgGrabberException($"Failed to extract port number from {_settings.EpgUrl}: {ex.Message}", ex);
            }
        }

        private byte[] UnzipData(byte[] data)
        {
            using (var outputStream = new MemoryStream())
            using (var inputStream = new MemoryStream(data))
            {
                using (var zipInputStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    zipInputStream.CopyTo(outputStream);
                }
                return outputStream.ToArray();
            }
        }
    }
}