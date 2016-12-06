using System;
using log4net;
using Owin;
using SharedComponents.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Extensions.ExecutionContextScoping;
using WebUi.api;
using WebUi.api.Logging;

namespace WebUi
{
    public class DependencyConfig : BaseDependencyConfigurator
    {
        public DependencyConfig(ILog logger) : base(logger)
        {
        }

        public override void RegisterComponents(Container container)
        {
            container.Register<InMemoryLogger>(Lifestyle.Singleton);
            container.Register<PerformanceMeter>(Lifestyle.Singleton);
        }

        public override Type Module => typeof(Program);
        public override Type Settings => typeof(Settings);
    }

    public static class DependencyConfigExtension
    {
        public static void UseOwinContextInjector(this IAppBuilder app, Container container)
        {
            
            // Create an OWIN middleware to create an execution context scope
            app.Use(async (context, next) =>
            {
                using (var scope = container.BeginExecutionContextScope())
                {
                    await next.Invoke();
                }
            });
        }
    }
}