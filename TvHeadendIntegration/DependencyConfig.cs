using System;
using log4net;
using SharedComponents.DependencyInjection;
using SimpleInjector;
using TvHeadendIntegration.TvHeadend.Web;

namespace TvHeadendIntegration
{
    public class DependencyConfig : BaseDependencyConfigurator
    {
        public DependencyConfig(ILog logger) : base(logger)
        {
        }
        
        public override void RegisterComponents(Container container)
        {
            container.RegisterSingleton<Func<TvhCommunication>>(() => container.GetInstance<TvhCommunication>());
        }

        public override Type Module => typeof(Program);
        public override Type Settings => typeof(Settings);
    }
}