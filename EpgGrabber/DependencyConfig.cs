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
            container.Register<IWebDownloader, HttpWebDownloader>();
            container.RegisterDecorator<IWebDownloader, CachedWebDownloader>();
            container.Register<IFileDownloader, FileDownloader>();
            container.Register<IDownloader, Downloader>();
            container.Register<IGenreTranslator, TvhGenreTranslator>();
            container.RegisterInitializer<TvhGenreTranslator>(c => c.Load());
        }

        public override Type Module => typeof(Program);
        public override Type Settings => typeof(Settings);
    }
}