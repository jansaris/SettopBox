using System;
using log4net;
using SharedComponents.DependencyInjection;
using SimpleInjector;
using WebHelper;

namespace ChannelList
{
    public class DependencyConfig : BaseDependencyConfigurator
    {
        public DependencyConfig(ILog logger) : base(logger)
        {
        }

        public override void RegisterComponents(Container container)
        {
            container.Register<ChannelList>();
            container.Register<JavascriptParser>();
            container.RegisterSingleton<Func<EpgWebClient>>(() => container.GetInstance<EpgWebClient>());
            container.Register<IWebDownloader, HttpWebDownloader>(Lifestyle.Singleton);
            container.Register<Compression>();
        }

        public override Type Module => typeof(Program);
        public override Type Settings => typeof(Settings);
    }
}