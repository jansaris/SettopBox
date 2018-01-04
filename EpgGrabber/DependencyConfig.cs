using System;
using EpgGrabber.IO;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Iptv;
using SimpleInjector;
using WebHelper;

namespace EpgGrabber
{
    public class DependencyConfig : BaseDependencyConfigurator
    {
        public DependencyConfig(ILog logger) : base(logger)
        {
        }

        public override void RegisterComponents(Container container)
        {
            WebHelper.DependencyConfig.RegisterComponents(container);
            container.RegisterDecorator<IWebDownloader, CachedWebDownloader>(Lifestyle.Singleton);
            container.RegisterInitializer<GenreTranslator>(c => c.Load());
            container.RegisterInitializer<ChannelList>(c => c.LoadChannelsFromDisk());
            container.RegisterInitializer<CachedWebDownloader>(c => c.LoadCache());
            container.Register<Func<IptvSocket>>(() => container.GetInstance<IptvSocket>);
        }

        public override Type Module => typeof(Program);
        public override Type Settings => typeof(Settings);
    }
}