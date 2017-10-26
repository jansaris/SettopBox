using System;
using log4net;
using SharedComponents.DependencyInjection;
using SimpleInjector;

namespace ChannelList
{
    public class DependencyConfig : BaseDependencyConfigurator
    {
        public DependencyConfig(ILog logger) : base(logger)
        {
        }

        public override void RegisterComponents(Container container)
        {
            container.Register<ChannelList>();
            container.Register<RtspDataReceiver>();
            container.Register<RtspDataParser>();
            container.Register<IptvChannel>();
            container.Register<IptvSocket>();
            container.Register<Func<IptvSocket>>(() => container.GetInstance<IptvSocket>);
            container.Register<Func<IptvChannel>>(() => container.GetInstance<IptvChannel>);
        }

        public override Type Module => typeof(Program);
        public override Type Settings => typeof(Settings);
    }
}