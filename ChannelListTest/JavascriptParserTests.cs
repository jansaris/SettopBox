using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChannelList;
using FluentAssertions;
using log4net;
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

            var ned1 = result.FirstOrDefault(e => e.Key == "ned1");
            ned1.Should().NotBeNull();
            ned1.Name.Should().Be("NPO 1");
            ned1.Icons.Count.Should().Be(1);
            ned1.Icons.First().Should().Be("npotv1.png");
            ned1.Locations.Count.Should().Be(5);
            ned1.Locations.First(l => l.Name == "HD+").Url.Should().Be("igmp://224.0.251.124:8248");
            ned1.Locations.First(l => l.Name == "HD").Url.Should().Be("igmp://224.0.252.126:7252");
            ned1.Locations.First(l => l.Name == "SD").Url.Should().Be("igmp://224.0.251.1:8002");
            ned1.Locations.First(l => l.Name == "ztv-hd").Url.Should().Be("igmp://239.193.252.126:7252");
            ned1.Locations.First(l => l.Name == "ztv-sd").Url.Should().Be("igmp://239.192.4.101:6202");
        }

        [Test]
        public void ItShouldParseNed2()
        {
            var result = ParseFullScript();

            var ned1 = result.FirstOrDefault(e => e.Key == "ned2");
            ned1.Should().NotBeNull();
            ned1.Name.Should().Be("NPO 2");
            ned1.Icons.Count.Should().Be(1);
            ned1.Icons.First().Should().Be("npotv2.png");
            ned1.Locations.Count.Should().Be(5);
            ned1.Locations.First(l => l.Name == "HD+").Url.Should().Be("igmp://224.0.251.125:8250");
            ned1.Locations.First(l => l.Name == "HD").Url.Should().Be("igmp://224.0.252.127:7254");
            ned1.Locations.First(l => l.Name == "SD").Url.Should().Be("igmp://224.0.251.2:8004");
            ned1.Locations.First(l => l.Name == "ztv-hd").Url.Should().Be("igmp://239.193.252.127:7254");
            ned1.Locations.First(l => l.Name == "ztv-sd").Url.Should().Be("igmp://239.192.4.102:6204");
        }

        [Test]
        public void ItShouldParseDiscovery()
        {
            var result = ParseFullScript();

            var disc = result.FirstOrDefault(e => e.Key == "discovery");
            disc.Should().NotBeNull();
            disc.Name.Should().Be("Discovery");
            disc.Icons.Count.Should().Be(3);
            disc.Icons.Any(i => i == "discovery.png").Should().BeTrue();
            disc.Icons.Any(i => i == "discovery_wit.png").Should().BeTrue();
            disc.Icons.Any(i => i == "discovery_thumb.png").Should().BeTrue();
            disc.Locations.Count.Should().Be(3);
            disc.Locations.Any(l => l.Name == null && l.Url == "igmp://224.0.252.129:7258" && l.RtpSkip).Should().BeTrue();
            disc.Locations.Any(l => l.Name == "ztv" && l.Url == "igmp://239.192.4.114:6228" && !l.RtpSkip).Should().BeTrue();
            disc.Locations.Any(l => l.Name == null && l.Url == "igmp://224.0.251.18:8036" && l.RtpSkip).Should().BeTrue();
        }

        private List<ChannelInfo> ParseFullScript()
        {
            return _fullScriptParseResult ?? (_fullScriptParseResult = _module.ParseChannels(_script));
        }
    }
}
