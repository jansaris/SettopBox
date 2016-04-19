using System;
using log4net;
using SharedComponents;
using SharedComponents.DependencyInjection;
using SimpleInjector;

namespace Keyblock
{
    public class DependencyConfig : BaseDependencyConfigurator
    {
        public DependencyConfig(ILog logger) : base(logger)
        {
        }

        public override void RegisterComponents(Container container)
        {
            container.Register<Settings>(Lifestyle.Singleton);
            container.Register<SslTcpClient>();
            container.Register<Keyblock>();
            container.Register<X509CertificateRequest>();
        }

        public override Type Module => typeof (Program);
        public override Type Settings => typeof(Settings);
    }
}