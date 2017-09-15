using System;
using log4net;

namespace Keyblock
{
    public static class ProtocolFactory
    {
        public static IProtocol CreateProtocol(Settings settings, ILog logger)
        {
            switch (settings.MessageFormat)
            {
                case "1154": return new Protocol1154(settings, logger);
                case "1155": return new Protocol1155(settings, logger);
                default: throw new ArgumentException($"MessageFormat '{settings.MessageFormat}' is not (yet?) supported");
            } 
        }
    }
}
