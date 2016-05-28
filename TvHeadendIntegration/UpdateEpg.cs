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

        public bool SendToTvheadend(string epgFile)
        {
            var file = ValidateEpgFile(epgFile);
            return WriteXmlToSocket(file);
        }

        FileInfo ValidateEpgFile(string epgFile)
        {
            var file = new FileInfo(epgFile);
            if (file.Exists)
            {
                _logger.Warn($"EPG file {file.FullName} doesn't exists");
                return null;
            }
            return file;
        }

        bool WriteXmlToSocket(FileInfo epgFile)
        {
            if (epgFile == null) return false;
            try
            {
                EndPoint ep = new UnixEndPoint(_settings.XmlTvSocket);
                using (var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP))
                {
                    socket.Connect(ep);
                    socket.Send(File.ReadAllBytes(epgFile.FullName));
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