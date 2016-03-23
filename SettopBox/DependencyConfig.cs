using System;
using log4net;
using SettopBox;
using SharedComponents.DependencyInjection;
using SimpleInjector;

namespace SettopBox
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