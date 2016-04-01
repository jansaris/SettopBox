using System;
using log4net;
using Owin;
using SharedComponents.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Extensions.ExecutionContextScoping;

namespace WebUi
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