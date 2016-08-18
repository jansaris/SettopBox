using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using log4net.Config;
using SharedComponents.Helpers;
using SharedComponents.Module;
using SharedComponents.Settings;
using SimpleInjector;
using SimpleInjector.Advanced;

namespace SharedComponents.DependencyInjection
{
    public static class SharedContainer
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(SharedContainer));

        static Container Create(string log4NetConfig)
        {
            XmlConfigurator.ConfigureAndWatch(new FileInfo(log4NetConfig));
            Logger.Debug("Create container");
            var container = new Container();
            container.Register<IThreadHelper, ThreadHelper>(Lifestyle.Singleton);
            container.RegisterLog4Net();
            return container;
        }

        public static Container CreateAndFill<T>(string log4Netconfig) 
            where T : BaseDependencyConfigurator
        {
            return CreateAndFill(log4Netconfig, typeof(T));
        }

        public static Container CreateAndFill<T1, T2, T3, T4, T5, T6, T7>(string log4Netconfig) 
            where T1 : BaseDependencyConfigurator
            where T2 : BaseDependencyConfigurator
            where T3 : BaseDependencyConfigurator
            where T4 : BaseDependencyConfigurator
            where T5 : BaseDependencyConfigurator
            where T6 : BaseDependencyConfigurator
            where T7 : BaseDependencyConfigurator
        {
            return CreateAndFill(log4Netconfig, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));
        }

        static Container CreateAndFill(string log4Netconfig, params Type[] configurators)
        {
            var container = Create(log4Netconfig);
            container.Register<LinuxSignal>(Lifestyle.Singleton);
            container.RegisterInitializer<LinuxSignal>(signal => signal.Listen());

            var modules = new List<Type>();
            foreach (var type in configurators)
            {
                Logger.Debug($"Register components for {type.FullName}");
                var configurator = CreateConfigurator(type);
                configurator.RegisterComponents(container);
                if(configurator.Module != null) modules.Add(configurator.Module);
                if (configurator.Settings != null)
                {
                    container.Register(configurator.Settings, configurator.Settings, Lifestyle.Singleton);
                    container.AppendToCollection(typeof(IniSettings), configurator.Settings);
                }
            }
            container.RegisterCollection<IModule>(modules);
            container.RegisterSingleton<ModuleCommunication>();
#if DEBUG
            Logger.Debug("Verify container");
            container.Verify(VerificationOption.VerifyOnly);
#endif
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