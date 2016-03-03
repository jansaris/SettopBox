using System;
using log4net;
using SharedComponents.DependencyInjection;
using SimpleInjector;

namespace Keyblock
{
    public class DependencyConfig : BaseDependencyConfigurator
    {
        public DependencyConfig(ILog logger) : base(logger)
        {
        }

        public override void RegisterComponents(Container container)
        {
            container.Register<Program>();
            container.Register<IniSettings>(Lifestyle.Singleton);
            container.Register<SslTcpClient>();
            container.Register<IKeyblock, Keyblock>();
        }
    }
}