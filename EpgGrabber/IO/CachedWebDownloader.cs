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
        static ILog _logger;
        static readonly List<CacheObject> Cache = new List<CacheObject>();

        readonly IWebDownloader _webDownloader;

        public CachedWebDownloader(ILog logger, IWebDownloader webDownloader)
        {
            _logger = logger;
            _webDownloader = webDownloader;
        }

        public byte[] DownloadBinary(string url)
        {
            var data = GetFromCache(url);
            if (data != null) return data.ByteData;
            var webData = _webDownloader.DownloadBinary(url);
            if (webData != null) Cache.Add(new CacheObject(url, webData));
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
            Cache.Add(obj);
        }

        private CacheObject GetFromCache(string url)
        {
            var cacheObj = Cache.FirstOrDefault(c => c.Url == url);
            if (cacheObj != null)
            {
                _logger.DebugFormat("Load from cache for {0}", url);
            }
            return cacheObj;
        }

        public static void LoadCache(string path)
        {
            var file = Path.Combine(path, Filename);
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
                        Cache.Add(obj);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load http cache from {file}", ex);
            }
        }

        public static void SaveCache(string path, int daysToCache)
        {
            var file = Path.Combine(path, Filename);
            try
            {
                var data = Cache;
                if (daysToCache > 0)
                {
                    _logger.Debug($"Filter the current cache to {daysToCache} days");
                    data = Cache.Where(c => c.Date.AddDays(daysToCache) >= DateTime.Now).ToList();
                }
                _logger.Debug($"Save cache to {file}");
                using (var writer = new BinaryWriter(File.OpenWrite(file)))
                {
                    _logger.Debug($"Write {data.Count} objects to cache file");
                    writer.Write(data.Count);
                    data.ForEach(d => d.Serialize(writer));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save http cache to {file}", ex);
            }
        }
    }
}
