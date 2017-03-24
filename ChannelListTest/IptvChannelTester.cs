using FakeItEasy;
using FluentAssertions;
using log4net;
using NUnit.Framework;
using System;
using WebUi.api.Iptv;

namespace ChannelListTest
{
    [TestFixture]
    class IptvChannelTester
    {
        [Test]
        public void FindNpo1()
        {
            var logger = A.Fake<ILog>();
            A.CallTo(() => logger.Info(A<string>._)).Invokes(Console.WriteLine);
            A.CallTo(() => logger.Debug(A<string>._)).Invokes(Console.WriteLine);
            var channelInfo = new IptvChannel(logger, () => new IptvSocket(logger));
            var info = channelInfo.ReadInfo("igmp://224.0.251.124:8248", "Npo1");
            info.Number.Should().Be(661);
            info.Provider.Should().Be("KPN");
            info.Name.Should().Be("NPO 1 HD glas");
        }

        [Test]
        public void FindNpo2()
        {
            var logger = A.Fake<ILog>();
            A.CallTo(() => logger.Info(A<string>._)).Invokes(Console.WriteLine);
            var channelInfo = new IptvChannel(logger, () => new IptvSocket(logger));
            var info = channelInfo.ReadInfo("igmp://224.0.251.125:8250", "Npo2");
            info.Number.Should().Be(662);
            info.Provider.Should().Be("KPN");
            info.Name.Should().Be("NPO 2 HD glas");
        }
    }
}
