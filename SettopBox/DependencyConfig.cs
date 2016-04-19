using System;
using log4net;
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

        public override Type Module => null;
        public override Type Settings => typeof(Settings);
    }
}