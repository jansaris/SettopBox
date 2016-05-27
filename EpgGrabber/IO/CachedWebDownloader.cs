using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace EpgGrabber.IO
{
    public class CachedWebDownloader : IWebDownloader
    {
        const string Filename = "HttpCache.dat";
        readonly ILog _logger;
        readonly Dictionary<string,CacheObject> _cache = new Dictionary<string, CacheObject>();

        readonly IWebDownloader _webDownloader;
        readonly Settings _settings;

        public CachedWebDownloader(ILog logger, IWebDownloader webDownloader, Settings settings)
        {
            _logger = logger;
            _webDownloader = webDownloader;
            _settings = settings;
        }

        public byte[] DownloadBinary(string url)
        {
            var data = GetFromCache(url);
            if (data != null) return data.ByteData;
            var webData = _webDownloader.DownloadBinary(url);
            if (webData != null) _cache.Add(url, new CacheObject(url, webData));
            return webData;
        }

        public string DownloadString(string url)
        {
            var data = GetFromCache(url);
            if (data != null) return data.StringData;
            var webData = _webDownloader.DownloadString(url);
            if (webData != null) AddToCache(new CacheObject(url, webData));
            return webData;
        }

        void AddToCache(CacheObject obj)
        {
            _logger.DebugFormat("Add {0} data to cache for {1}", obj.DataType, obj.Url);
            _cache.Add(obj.Url, obj);
        }

        CacheObject GetFromCache(string url)
        {
            if (!_cache.ContainsKey(url)) return null;
            _logger.DebugFormat("Load from cache for {0}", url);
            return _cache[url];
        }

        public void LoadCache()
        {
            var file = Path.Combine(_settings.DataFolder, Filename);
            try
            {
                if (!File.Exists(file))
                {
                    _logger.WarnFormat("HTTP Cache file {0} doesn't exist", file);
                    return;
                }
                _logger.DebugFormat("Load {0} cache into memory", file);
                using (var reader = new BinaryReader(File.OpenRead(file)))
                {
                    var count = reader.ReadInt32();
                    _logger.DebugFormat("Read {0} objects into cache", count);
                    for(var i = 0; i < count; i ++)
                    {
                        var obj = new CacheObject();
                        obj.Deserialize(reader);
                        _cache.Add(obj.Url, obj);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load http cache from {file}", ex);
            }
        }

        public void SaveCache()
        {
            var file = Path.Combine(_settings.DataFolder, Filename);
            try
            {
                var data = _cache;
                if (_settings.NumberOfEpgDays > 0)
                {
                    _logger.Debug($"Filter the current cache to {_settings.NumberOfEpgDays} days");
                    data = _cache.Where(c => c.Value.Date.AddDays(_settings.NumberOfEpgDays) >= DateTime.Now).ToDictionary(c => c.Key, c => c.Value);
                }
                _logger.Debug($"Save cache to {file}");
                using (var writer = new BinaryWriter(File.OpenWrite(file)))
                {
                    _logger.Debug($"Write {data.Count} objects to cache file");
                    writer.Write(data.Count);
                    foreach (var keyvalue in data)
                    {
                        keyvalue.Value.Serialize(writer);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save http cache to {file}", ex);
            }
        }
    }
}
