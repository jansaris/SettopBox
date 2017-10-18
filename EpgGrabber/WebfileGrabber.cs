using System;
using System.IO;
using System.Net;
using log4net;

namespace EpgGrabber
{
    public class WebfileGrabber : IGrabber
    {
        readonly ILog _logger;
        readonly Settings _settings;

        public WebfileGrabber(ILog logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public string Download(Func<bool> stopProcessing)
        {
            if (stopProcessing()) return null;
            var file = Path.Combine(_settings.DataFolder, _settings.XmlTvFileName);
            _logger.Info($"Start grabbing EPG from {_settings.EpgUrl} into {file}");
            try
            {
                var request = WebRequest.Create(_settings.EpgUrl);
                var response = request.GetResponse();
                var stream = response.GetResponseStream();
                var buffer = new byte[2048];
                long totalLength = 0;
                var mbs = 0;
                using (var output = File.OpenWrite(file))
                {
                    while (stream?.CanRead ?? false)
                    {
                        if (stopProcessing()) return null;

                        var length = stream.Read(buffer, 0, buffer.Length);
                        if (length == 0) break;
                        output.Write(buffer, 0 , length);

                        totalLength += length;
                        var newMbs = (int)totalLength / 1024 / 1024;

                        if (mbs == newMbs) continue;

                        mbs = newMbs;
                        _logger.Info($"Downloaded {mbs}Mb");
                    } 
                    output.Flush(true);
                }
                
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to download {_settings.EpgUrl} into {file}: {ex.Message}");
                return null;
            }


            return file;
        }
    }
}