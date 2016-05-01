using System;
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
            
        }

        public override Type Module => typeof(Program);
        public override Type Settings => typeof(Settings);
    }
}