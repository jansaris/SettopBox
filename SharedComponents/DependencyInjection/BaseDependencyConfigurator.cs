using System;
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

        public abstract Type Module { get; }
        public abstract Type Settings { get; }
    }
}