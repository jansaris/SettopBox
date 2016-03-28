using log4net;
using SharedComponents.DependencyInjection;
using SimpleInjector;

namespace WebUi
{
    public class DependencyConfig : BaseDependencyConfigurator
    {
        public DependencyConfig(ILog logger) : base(logger)
        {
        }

        public override void RegisterComponents(Container container)
        {
            container.Register<Program>();
        }
    }
}