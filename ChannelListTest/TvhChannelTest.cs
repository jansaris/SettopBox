using FakeItEasy;
using FluentAssertions;
using log4net;
using NUnit.Framework;
using System;
using TvHeadendIntegration.TvHeadend;
using TvHeadendIntegration.TvHeadend.Web;

namespace ChannelListTest
{
    [TestFixture]
    public class TvhChannelTest
    {
        TvhModel configuration;
        ILog _logger;
        TvHeadendIntegration.Settings _settings;

        [SetUp]
        public void SetUp()
        {
            _logger = A.Fake<ILog>();
            _settings = new TvHeadendIntegration.Settings(_logger)
            {
                
            };
            configuration = new TvhModel(_logger, _settings, () => new TvhCommunication(_logger, _settings));
        }

        [Test]
        public void ReadGlashardNetwork()
        {
            configuration.ReadFromWeb();

            configuration.Should().NotBeNull();
            configuration.ResolveChannel("NPO1").number.Should().NotBe(-1);
        }

        [Test]
        public void LoadNode()
        {
            var comm = new TvhCommunication(_logger, _settings);
            var response = comm.Post("/api/idnode/load", "uuid=f9a34473d1930d2ecd82c10ccbdb42f7&meta=1", null);
            response.Should().NotBeNull();
        }

        //[Test]
        public void UpdateAMux()
        {
            configuration.ReadFromWeb();

            configuration.Should().NotBeNull();
            var mux = configuration.ResolveMux("Ziggo Sport Extra 1");
            mux.State.Should().Be(State.Loaded);
            mux.iptv_sname.Should().Be("Ziggo Sport Extra 1");
            mux.iptv_sname = "Ziggo Sport Extra 12";
            configuration.UpdateOnTvh(mux);

            configuration.ReadFromWeb();
            mux = configuration.ResolveMux("Ziggo Sport Extra 1");
            mux.State.Should().Be(State.Loaded);
            mux.iptv_sname.Should().Be("Ziggo Sport Extra 12");
            mux.iptv_sname = "Ziggo Sport Extra 1";
            configuration.UpdateOnTvh(mux);
        }

        //[Test]
        public void AddChannel()
        {
            //Add channel 'rtlz' with url 'rtp://224.0.251.115:8230' to TvHeadend
            TvhCommunication Factory() => new TvhCommunication(_logger, _settings);

            var model = new TvhModel(_logger, _settings, Factory);

            model.ReadFromWeb();

            model.AddChannel("rtlz", "rtp://224.0.251.115:8230", true);


        }
    }
}
