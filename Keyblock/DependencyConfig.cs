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
            container.Register<Settings>(Lifestyle.Singleton);
            container.Register<SslTcpClient>();
            container.Register<Keyblock>();
        }
    }
}