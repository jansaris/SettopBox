using System;
using System.Threading.Tasks;
using ChannelList;
using FakeItEasy;
using FluentAssertions;
using log4net;
using NUnit.Framework;
using SharedComponents.Models;

namespace ChannelListTest
{
    [TestFixture]
    public class IptvChannelTest
    {
        private IptvChannel _iptvChannel;

        [SetUp]
        public void SetUp()
        {
            var logger = A.Fake<ILog>();
            A.CallTo(() => logger.Info(A<string>._)).Invokes(Console.WriteLine);
            A.CallTo(() => logger.Debug(A<string>._)).Invokes(Console.WriteLine);
            _iptvChannel = new IptvChannel(logger, () => new IptvSocket());
        }

        [Test]
        public void FindNpo1()
        {    
            var location = new ChannelLocation
            {
                Host = "224.0.251.124",
                Port = 8248,
                Protocol = "rtp"
            };
            var info = _iptvChannel.ReadInfo(location, "Npo1");
            info.Number.Should().Be(661);
            info.Name.Should().Be("NPO 1 HD glas");
        }

        [Test]
        public void FindNpo2()
        {
            var location = new ChannelLocation
            {
                Host = "224.0.251.125",
                Port = 8250,
                Protocol = "rtp"
            };
            var info = _iptvChannel.ReadInfo(location, "Npo2");
            info.Number.Should().Be(662);
            info.Name.Should().Be("NPO 2 HD glas");
        }

        [Test]
        public void FindRtl4()
        {
            var location = new ChannelLocation
            {
                Host = "224.0.251.134",
                Port = 8268,
                Protocol = "rtp"
            };

            var info = _iptvChannel.ReadInfo(location, "Rtl4");
            info.Number.Should().Be(664);
            info.Name.Should().Be("RTL4 HD glas");
        }

        [Test]
        public void FindRtl5()
        {
            var location = new ChannelLocation
            {
                Host = "224.0.251.135",
                Port = 8270,
                Protocol = "rtp"
            };

            var info = _iptvChannel.ReadInfo(location, "Rtl5");

            info.Number.Should().Be(665);
            info.Name.Should().Be("RTL5 HD glas");
        }
    }
}