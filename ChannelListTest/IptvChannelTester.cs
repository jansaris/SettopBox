using FakeItEasy;
using FluentAssertions;
using log4net;
using NUnit.Framework;
using System;

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
            var channelInfo = new WebUi.api.Iptv.IptvChannel(logger);
            var info = channelInfo.ReadInfo("igmp://224.0.251.124:8248");
            info.Number.Should().Be(661);
            info.Provider.Should().Be("KPN");
            info.Name.Should().Be("NPO 1 HD glas");
        }

        [Test]
        public void FindNpo2()
        {
            var logger = A.Fake<ILog>();
            A.CallTo(() => logger.Info(A<string>._)).Invokes(Console.WriteLine);
            var channelInfo = new WebUi.api.Iptv.IptvChannel(logger);
            var info = channelInfo.ReadInfo("igmp://224.0.251.125:8250");
            info.Number.Should().Be(662);
            info.Provider.Should().Be("KPN");
            info.Name.Should().Be("NPO 2 HD glas");
        }
    }
}
