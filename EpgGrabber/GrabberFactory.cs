using System;
using SimpleInjector;

namespace EpgGrabber
{
    public class GrabberFactory
    {
        private readonly Container _container;
        private readonly Settings _settings;

        public GrabberFactory(Container container, Settings settings)
        {
            _container = container;
            _settings = settings;
        }
        public IGrabber Create()
        {
            switch (_settings.Type)
            {
                case "Kpn": return _container.GetInstance<KpnGrabber>();
                case "Obn": return _container.GetInstance<ObnGrabber>();
                case "Webfile": return _container.GetInstance<WebfileGrabber>();
                case "ItvOnline": return _container.GetInstance<ItvOnlineGrabber>();
                    default: throw new NotSupportedException($"Grabber '{_settings.Type}' is not supported");
            }
        }
    }
}