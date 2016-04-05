using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using log4net.Config;
using SharedComponents.Module;
using SimpleInjector;

namespace SharedComponents.DependencyInjection
{
    public static class SharedContainer
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(SharedContainer));

        static Container Create(string log4NetConfig)
        {
            XmlConfigurator.Configure(new FileInfo(log4NetConfig));
            Logger.Debug("Create container");
            var container = new Container();
            container.RegisterLog4Net();
            return container;
        }

        public static Container CreateAndFill<T>(string log4Netconfig) 
            where T : BaseDependencyConfigurator
        {
            return CreateAndFill(log4Netconfig, typeof(T));
        }

        public static Container CreateAndFill<T1, T2, T3, T4>(string log4Netconfig) 
            where T1 : BaseDependencyConfigurator
            where T2 : BaseDependencyConfigurator
            where T3 : BaseDependencyConfigurator
            where T4 : BaseDependencyConfigurator
        {
            return CreateAndFill(log4Netconfig, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
        }

        static Container CreateAndFill(string log4Netconfig, params Type[] configurators)
        {
            var container = Create(log4Netconfig);
            var modules = new List<Type>();
            foreach (var type in configurators)
            {
                Logger.Debug($"Register components for {type.FullName}");
                var configurator = CreateConfigurator(type);
                configurator.RegisterComponents(container);
                if(configurator.Module != null) modules.Add(configurator.Module);
            }
            container.RegisterCollection<IModule>(modules);
            container.RegisterSingleton<ModuleInformation>();
            Logger.Debug("Verify container");
            container.Verify(VerificationOption.VerifyOnly);
            Logger.Debug("Created container");
            return container;
        }

        static BaseDependencyConfigurator CreateConfigurator(Type configuratorType)
        {
            var logger = LogManager.GetLogger(configuratorType);
            return (BaseDependencyConfigurator)Activator.CreateInstance(configuratorType, logger);
        }
    }
}