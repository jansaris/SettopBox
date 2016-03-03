using log4net;
using SimpleInjector;

namespace SharedComponents.DependencyInjection
{
    public abstract class BaseDependencyConfigurator
    {
        protected readonly ILog Logger;

        protected BaseDependencyConfigurator(ILog logger)
        {
            Logger = logger;
        }
        public abstract void RegisterComponents(Container container);
    }
}