using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChannelList;
using FakeItEasy;
using FluentAssertions;
using log4net;
using NUnit.Framework;

namespace ChannelListTest
{
    [TestFixture]
    public class RtspDataParserTest
    {
        private RtspDataParser _parser;
        private byte[] _data;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            _data = File.ReadAllBytes("RtspData.dat");
        }

        [SetUp]
        public void SetUp()
        {
            _parser = new RtspDataParser(A.Fake<ILog>());
        }

        [Test]
        public void ParseChannelsShouldParse234Channels()
        {
            var channels = _parser.ParseChannels(_data);
            channels.Count.Should().Be(234);
        }

        [Test]
        public void ParseChannelsShouldRemoveHDfromChannelName()
        {
            var channels = _parser.ParseChannels(_data);
            channels.Any(c => c.Name.EndsWith("HD")).Should().BeFalse();
        }

        [Test]
        public void ParseChannelsShouldRemoveGlasfromChannelName()
        {
            var channels = _parser.ParseChannels(_data);
            channels.Any(c => c.Name.EndsWith("Glas")).Should().BeFalse();
        }

        [Test]
        public void ParseChannelsShouldRemoveGlasAndHDfromChannelName()
        {
            var channels = _parser.ParseChannels(_data);
            var npo1 = channels.Where(c => c.Name.StartsWith("NPO 1")).ToList();
            npo1.Any(c => c.Name.Contains("Glas")).Should().BeFalse();
            npo1.Any(c => c.Name.Contains("HD")).Should().BeFalse();
        }

        [Test]
        public void ParseChannelsShouldExtractTheChannelName()
        {
            var channels = _parser.ParseChannels(_data);
            channels[0].Name.Should().Be("FOX Ered 1");
            channels[10].Name.Should().Be("RTL 4");
            channels[100].Name.Should().Be("FunX");
        }

        [Test]
        public void ParseChannelsShouldMergeTheChannels()
        {
            var channels = _parser.ParseChannels(_data);
            channels.Count(c => c.Name == "NPO 1").Should().Be(1);
        }

        [Test]
        public void ParseChannelsShouldMergeTheChannelLocations()
        {
            var channels = _parser.ParseChannels(_data);
            var npo1 = channels.First(c => c.Name == "NPO 1");
            npo1.Locations.Count.Should().Be(3);
        }

        [Test]
        public void ParseChannelsShouldExtractTheUrl()
        {
            var channels = _parser.ParseChannels(_data);
            var npo1 = channels.First(c => c.Name == "NPO 1");
            /*
             m=video 8248 RTP/AVPF 96
             i=Original Source Stream
             c=IN IP4 224.0.251.124/255
             b=AS:12750
            */
            npo1.Locations.Any(c => c.Url == "rtp://224.0.251.124:8248").Should().BeTrue();
        }

        [Test]
        public void ParseChannelsShouldExtractTheBitrates()
        {
            var channels = _parser.ParseChannels(_data);
            var npo1 = channels.First(c => c.Name == "NPO 1");
            /*
             m=video 8248 RTP/AVPF 96
             i=Original Source Stream
             c=IN IP4 224.0.251.124/255
             b=AS:12750
            */
            npo1.Locations.Any(c => c.Bitrate == 12750).Should().BeTrue();
        }

        [Test]
        public void ParseChannelsShouldSortTheLocationsByBitrate()
        {
            var channels = _parser.ParseChannels(_data);
            var npo1 = channels.First(c => c.Name == "NPO 1");
            
            npo1.Locations[0].Bitrate.Should().Be(12750);
            npo1.Locations[1].Bitrate.Should().Be(7100);
            npo1.Locations[2].Bitrate.Should().Be(3200);

        }

        [Test]
        public void ParseChannelsShouldTagRadioChannelsAsRadio()
        {
            var channels = _parser.ParseChannels(_data);
            channels.First(c => c.Name == "NPO Radio 1").Radio.Should().BeTrue();
            channels.First(c => c.Name == "BBC Radio 1").Radio.Should().BeTrue();
            channels.First(c => c.Name == "FunX").Radio.Should().BeTrue();
            channels.First(c => c.Name == "100%NL").Radio.Should().BeTrue();
        }

        [Test]
        public void ParseChannelsShouldTagVideoChannelsAsRadio()
        {
            var channels = _parser.ParseChannels(_data);
            channels.First(c => c.Name == "NPO 1").Radio.Should().BeFalse();
            channels.First(c => c.Name == "RTL 4").Radio.Should().BeFalse();
            channels.First(c => c.Name == "Nick Jr.").Radio.Should().BeFalse();
        }
    }
}