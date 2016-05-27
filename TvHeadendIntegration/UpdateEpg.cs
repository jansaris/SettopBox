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
            return WriteXmlToSocket(epgFile);
        }

        bool WriteXmlToSocket(string epgFile)
        {
            try
            {
                EndPoint ep = new UnixEndPoint(_settings.XmlTvSocket);
                using (var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP))
                {
                    socket.Connect(ep);
                    socket.Send(File.ReadAllBytes(epgFile));
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