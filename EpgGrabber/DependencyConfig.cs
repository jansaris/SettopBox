using System;
using EpgGrabber.IO;
using log4net;
using SharedComponents.DependencyInjection;
using SimpleInjector;

namespace EpgGrabber
{
    public class DependencyConfig : BaseDependencyConfigurator
    {
        public DependencyConfig(ILog logger) : base(logger)
        {
        }

        public override void RegisterComponents(Container container)
        {
            container.Register<IWebDownloader, HttpWebDownloader>(Lifestyle.Singleton);
            container.RegisterDecorator<IWebDownloader, CachedWebDownloader>(Lifestyle.Singleton);
            container.Register<IFileDownloader, FileDownloader>(Lifestyle.Singleton);
            container.Register<IDownloader, Downloader>();
            container.RegisterInitializer<GenreTranslator>(c => c.Load());
            container.RegisterInitializer<ChannelList>(c => c.LoadChannelsFromDisk());
            container.RegisterInitializer<CachedWebDownloader>(c => c.LoadCache());
        }

        public override Type Module => typeof(Program);
        public override Type Settings => typeof(Settings);
    }
}