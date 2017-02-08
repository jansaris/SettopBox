using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChannelList;
using FluentAssertions;
using NUnit.Framework;
using SharedComponents.Module;

namespace ChannelListTest
{
    [TestFixture]
    public class JavascriptParserTests : BaseTestWithLogging
    {
        private JavascriptParser _module;
        private string _script;
        private string _scriptNed1;

        private static List<ChannelInfo> _fullScriptParseResult;

        [SetUp]
        public void SetUp()
        { 
            _module = new JavascriptParser(Logger);
            _script = File.ReadAllText("code.js.txt");
            _scriptNed1 = File.ReadAllText("code.js_ned1_part.txt");
        }

        [Test]
        public void ItShouldReturnNoChannelsIfTheScriptIsEmpty()
        {
            var result = _module.ParseChannels("");
            result.Count.Should().Be(0);
        }

        [Test]
        public void ItShouldParseNed1()
        {
            var result = _module.ParseChannels(_scriptNed1);

            var channel = result.FirstOrDefault(e => e.Key == "ned1");
            channel.Should().NotBeNull();
            Assert.IsNotNull(channel);
            channel.Name.Should().Be("NPO 1");
            channel.Number.Should().Be(1);
            channel.Icons.Count.Should().Be(1);
            channel.Icons.First().Should().Be("npotv1.png");
            channel.Locations.Count.Should().Be(5);
            channel.Locations.First(l => l.Name == "HD+").Url.Should().Be("igmp://224.0.251.124:8248");
            channel.Locations.First(l => l.Name == "HD").Url.Should().Be("igmp://224.0.252.126:7252");
            channel.Locations.First(l => l.Name == "SD").Url.Should().Be("igmp://224.0.251.1:8002");
            channel.Locations.First(l => l.Name == "ztv-hd").Url.Should().Be("igmp://239.193.252.126:7252");
            channel.Locations.First(l => l.Name == "ztv-sd").Url.Should().Be("igmp://239.192.4.101:6202");
            channel.Radio.Should().BeFalse();
        }

        [Test]
        public void ItShouldParseNed2()
        {
            var result = ParseFullScript();

            var channel = result.FirstOrDefault(e => e.Key == "ned2");
            channel.Should().NotBeNull();
            Assert.IsNotNull(channel);
            channel.Name.Should().Be("NPO 2");
            channel.Number.Should().Be(2);
            channel.Icons.Count.Should().Be(1);
            channel.Icons.First().Should().Be("npotv2.png");
            channel.Locations.Count.Should().Be(5);
            channel.Locations.First(l => l.Name == "HD+").Url.Should().Be("igmp://224.0.251.125:8250");
            channel.Locations.First(l => l.Name == "HD").Url.Should().Be("igmp://224.0.252.127:7254");
            channel.Locations.First(l => l.Name == "SD").Url.Should().Be("igmp://224.0.251.2:8004");
            channel.Locations.First(l => l.Name == "ztv-hd").Url.Should().Be("igmp://239.193.252.127:7254");
            channel.Locations.First(l => l.Name == "ztv-sd").Url.Should().Be("igmp://239.192.4.102:6204");
            channel.Radio.Should().BeFalse();
        }

        [Test]
        public void ItShouldParseRtl5()
        {
            var result = ParseFullScript();

            var channel = result.FirstOrDefault(e => e.Key == "rtl5");
            channel.Should().NotBeNull();
            Assert.IsNotNull(channel);
            channel.Name.Should().Be("RTL 5");
            channel.Number.Should().Be(5);
            channel.Icons.Count.Should().Be(2);
            channel.Icons.First().Should().Be("rtl5.png");
            channel.Icons.Skip(1).First().Should().Be("rtl5_wit.png");
            channel.Locations.Count.Should().Be(4);
            channel.Locations.Any(l => l.Name == "HD+" && l.Url == "igmp://224.0.251.135:8270" && l.RtpSkip).Should().BeTrue();
            channel.Locations.Any(l => l.Name == "ztv" && l.Url == "igmp://239.192.4.105:6210" && !l.RtpSkip).Should().BeTrue();
            channel.Locations.Any(l => l.Name == null && l.Url == "igmp://224.0.252.137:7274" && l.RtpSkip).Should().BeTrue();
            channel.Locations.Any(l => l.Name == null && l.Url == "igmp://224.0.251.5:8010" && l.RtpSkip).Should().BeTrue();
            channel.Radio.Should().BeFalse();
        }

        [Test]
        public void ItShouldParseDiscovery()
        {
            var result = ParseFullScript();

            var channel = result.FirstOrDefault(e => e.Key == "discovery");
            channel.Should().NotBeNull();
            Assert.IsNotNull(channel);
            channel.Name.Should().Be("Discovery");
            channel.Number.Should().Be(61);
            channel.Icons.Count.Should().Be(3);
            channel.Icons.Any(i => i == "discovery.png").Should().BeTrue();
            channel.Icons.Any(i => i == "discovery_wit.png").Should().BeTrue();
            channel.Icons.Any(i => i == "discovery_thumb.png").Should().BeTrue();
            channel.Locations.Count.Should().Be(3);
            channel.Locations.Any(l => l.Name == null && l.Url == "igmp://224.0.252.129:7258" && l.RtpSkip).Should().BeTrue();
            channel.Locations.Any(l => l.Name == "ztv" && l.Url == "igmp://239.192.4.114:6228" && !l.RtpSkip).Should().BeTrue();
            channel.Locations.Any(l => l.Name == null && l.Url == "igmp://224.0.251.18:8036" && l.RtpSkip).Should().BeTrue();
            channel.Radio.Should().BeFalse();
        }

        [Test]
        public void ItShouldParseRadio1()
        {
            var result = ParseFullScript();

            var channel = result.FirstOrDefault(e => e.Key == "radio1");
            channel.Should().NotBeNull();
            Assert.IsNotNull(channel);
            channel.Name.Should().Be("NPO Radio 1");
            channel.Number.Should().Be(1001);
            channel.Icons.Count.Should().Be(1);
            channel.Icons.Any(i => i == "nporadio1.png").Should().BeTrue();
            channel.Locations.Count.Should().Be(2);
            channel.Locations.Any(l => l.Name == null && l.Url == "igmp://224.0.251.161:8322" && l.RtpSkip).Should().BeTrue();
            channel.Locations.Any(l => l.Name == "ztv" && l.Url == "igmp://239.193.251.161:8322" && !l.RtpSkip).Should().BeTrue();
            channel.Radio.Should().BeTrue();
        }

        [Test]
        public void ItShouldTagChannelsAsRadio()
        {
            var result = ParseFullScript();

            var leoRadio = result.First(c => c.Name == "LEO FM");
            leoRadio.Radio.Should().BeTrue();

            var hoornRadio = result.First(c => c.Name == "Radio Hoorn");
            hoornRadio.Radio.Should().BeTrue();
        }

        private List<ChannelInfo> ParseFullScript()
        {
            return _fullScriptParseResult ?? (_fullScriptParseResult = _module.ParseChannels(_script));
        }
    }
}
