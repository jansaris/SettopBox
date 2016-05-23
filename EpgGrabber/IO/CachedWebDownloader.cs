using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace EpgGrabber.IO
{
    public class CachedWebDownloader : IWebDownloader, IDisposable
    {
        readonly IWebDownloader _webDownloader;
        readonly ILog _logger;
        const string Filename = "HttpCache.dat";

        readonly string _file;
        readonly int _daysToCache;
        readonly List<CacheObject> _cache = new List<CacheObject>();
        bool _disposing;

        public CachedWebDownloader(ILog logger, Settings settings, IWebDownloader webDownloader)
        {
            _logger = logger;
            _webDownloader = webDownloader;
            _file = Path.Combine(settings.DataFolder, Filename);
            _daysToCache = settings.NumberOfEpgDays;
        }

        public byte[] DownloadBinary(string url)
        {
            var data = GetFromCache(url);
            if (data != null) return data.ByteData;
            var webData = _webDownloader.DownloadBinary(url);
            if (webData != null) _cache.Add(new CacheObject(url, webData));
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

        private void AddToCache(CacheObject obj)
        {
            _logger.DebugFormat("Add {0} data to cache for {1}", obj.DataType, obj.Url);
            _cache.Add(obj);
        }

        private CacheObject GetFromCache(string url)
        {
            var cacheObj = _cache.FirstOrDefault(c => c.Url == url);
            if (cacheObj != null)
            {
                _logger.DebugFormat("Load from cache for {0}", url);
            }
            return cacheObj;
        }

        public void LoadCache()
        {
            try
            {
                if (!File.Exists(_file))
                {
                    _logger.WarnFormat("HTTP Cache file {0} doesn't exist", _file);
                    return;
                }
                _logger.DebugFormat("Load {0} cache into memory", _file);
                using (var reader = new BinaryReader(File.OpenRead(_file)))
                {
                    var count = reader.ReadInt32();
                    _logger.DebugFormat("Read {0} objects into cache", count);
                    for(var i = 0; i < count; i ++)
                    {
                        var obj = new CacheObject();
                        obj.Deserialize(reader);
                        _cache.Add(obj);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load http cache from {_file}", ex);
            }
        }

        void SaveCache()
        {
            try
            {
                var data = _cache;
                if (_daysToCache > 0)
                {
                    _logger.Debug($"Filter the current cache to {_daysToCache} days");
                    data = _cache.Where(c => c.Date.AddDays(_daysToCache) >= DateTime.Now).ToList();
                }
                _logger.Debug($"Save cache to {_file}");
                using (var writer = new BinaryWriter(File.OpenWrite(_file)))
                {
                    _logger.Debug($"Write {data.Count} objects to cache file");
                    writer.Write(data.Count);
                    data.ForEach(d => d.Serialize(writer));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save http cache to {_file}", ex);
            }
        }

        public void Dispose()
        {
            if (_disposing) return;
            _disposing = true;
            SaveCache();
        }
    }
}
