using System;
using SimpleInjector;

namespace WebHelper
{
    public static class DependencyConfig
    {
        private static bool _registered;
        public static void RegisterComponents(Container container)
        {
            if (_registered) return;
            _registered = true;
            container.Register<IWebDownloader, HttpWebDownloader>(Lifestyle.Singleton);
            container.Register<IFileDownloader, FileDownloader>(Lifestyle.Singleton);
            container.Register<IDownloader, Downloader>();
            container.RegisterSingleton<Func<EpgWebClient>>(() => container.GetInstance<EpgWebClient>());
        }
    }
}