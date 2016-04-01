using System;
using log4net;
using SharedComponents;
using SharedComponents.DependencyInjection;
using SimpleInjector;

namespace NewCamd
{
    public class DependencyConfig : BaseDependencyConfigurator
    {
        public DependencyConfig(ILog logger) : base(logger)
        {
        }

        public override void RegisterComponents(Container container)
        {
            container.Register<IModule, Program>();
            container.Register<NewCamdApi>();
            container.Register<NewCamdCommunication>();
            container.RegisterSingleton<Func<NewCamdApi>>(() => container.GetInstance<NewCamdApi>());
        }
    }
}