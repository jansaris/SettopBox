using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using log4net;

namespace TvHeadendIntegration
{
    public class UpdateEpg
    {
        readonly ILog _logger;
        readonly Settings _settings;
        public UpdateEpg(ILog logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public bool SendToTvheadend(string file)
        {
            if (string.IsNullOrWhiteSpace(_settings.XmlTvSocket))
            {
                _logger.Info("No socket information for TvHeadend");
                return false;
            }
            var fileinfo = ValidateEpgFile(file);
            var result = WriteXmlToSocket(fileinfo);
            if(result) _logger.Info($"Successfully sended {file} to Tvheadend");
            return result;
        }

        FileInfo ValidateEpgFile(string epgFile)
        {
            var file = new FileInfo(epgFile);
            if (!file.Exists)
            {
                _logger.Warn($"EPG file {file.FullName} doesn't exists");
                return null;
            }
            return file;
        }

        bool WriteXmlToSocket(FileInfo file)
        {
            if (file == null) return false;
            try
            {
                EndPoint ep = new UnixEndPoint(_settings.XmlTvSocket);
                using (var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP))
                {
                    socket.Connect(ep);
                    socket.Send(File.ReadAllBytes(file.FullName));
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to write XML data to {_settings.XmlTvSocket}", ex);
            }
            return false;
        }
    }
}