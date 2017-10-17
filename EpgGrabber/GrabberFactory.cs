﻿using System;
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
                case "Kpn": throw new NotImplementedException();
                case "Obn": return _container.GetInstance<ObnGrabber>();
                case "Raw": return _container.GetInstance<RawGrabber>();
                    default: throw new NotSupportedException($"Grabber '{_settings.Type}' is not supported");
            }
        }
    }
}