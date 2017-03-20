using log4net;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;
using WebUi.api.Models;
using System.Threading.Tasks;
using SharedComponents.Module;
using System.Linq;
using SharedComponents.Models;
using System;
using WebUi.api.Iptv;

namespace WebUi.api.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/settopbox")]
    public class SettopBoxController : ApiController
    {
        readonly ILog _logger;
        readonly ModuleCommunication _info;
        readonly IptvChannel _channelTester;
        static List<Channel> _channels;

        public SettopBoxController(ILog logger, ModuleCommunication info, IptvChannel channelTester)
        {
            _logger = logger;
            _info = info;
            _channelTester = channelTester;
        }

        public IHttpActionResult Get()
        {
            _logger.Info("get all channels");
            LoadChannels(true);
            return Ok(_channels);
        }

        private void LoadChannels(bool force)
        {
            if (_channels != null && !force) return;
            var channels = _info.Data(nameof(ChannelList)) as ChannelListInfo;
            if (channels == null) return;
            _channels = channels.Channels.Select(Convert).ToList();

            if (_info.Data(nameof(EpgGrabber)) is EpgGrabberInfo epg)
            {
                _channels.ForEach(c =>
                {
                    if (epg.Channels.Contains(c.Name)) c.EpgGrabber = true;
                });
            }
        }

        private Channel Convert(ChannelInfo info)
        {
            return new Channel
            {
                Name = info.Name,
                Id = info.Key,
                Number = info.Number,
                AvailableChannels = info.Locations,
            };
        }

        public IHttpActionResult Get(string id)
        {
            _logger.Info($"get channel: {id}");
            var index = _channels.FindIndex(c => c.Id == id);
            if (index == -1) return NotFound();
            return Ok(_channels[index]);
        }

        public async Task<IHttpActionResult> Put(Channel channel)
        {
            try
            {
                _logger.Info($"Update channel: {channel?.Id}");
                LoadChannels(false);
                var index = _channels.FindIndex(c => c.Id == channel.Id);
                if (index == -1) return NotFound();
                await UpdateChannel(channel, _channels[index]);
                _channels[index] = channel;
                return Ok();
            }
            catch(Exception ex)
            {
                _logger.Error($"Failed to update {channel?.Id}: {ex.Message}", ex);
                return InternalServerError(ex);
            }
        }

        private async Task UpdateChannel(Channel newChannel, Channel oldChannel)
        {
            await UpdateKeyblock(newChannel, oldChannel);
            UpdateEpg(newChannel, oldChannel);
            await UpdateTvHeadend(newChannel, oldChannel);
        }

        private async Task UpdateTvHeadend(Channel newChannel, Channel oldChannel)
        {
            if (newChannel.TvHeadend)
            {
                _channelTester.ReadInfo(newChannel.TvHeadendChannel);
            }
            if(newChannel.TvHeadend != oldChannel.TvHeadend)
            {
                await Task.Delay(1000);
            }
            else if(newChannel.TvHeadendChannel != oldChannel.TvHeadendChannel)
            {
                await Task.Delay(500);
            }
        }

        private void UpdateEpg(Channel newChannel, Channel oldChannel)
        {
            if (newChannel.EpgGrabber == oldChannel.EpgGrabber) return;
            _logger.Info($"{(newChannel.EpgGrabber ? "Add to" : "Remove from")} EpgGrabber - {newChannel.Name}");
            var ecu = new EpgChannelUpdate { Id = newChannel.Id, Name = newChannel.Name, Enabled = newChannel.EpgGrabber };
            var data = new CommunicationData(DataType.EpgChannelUpdate, ecu);
            var thread = _info.SendData(nameof(WebUi), nameof(EpgGrabber), data);
            thread?.Join();
        }

        private async Task UpdateKeyblock(Channel newChannel, Channel oldChannel)
        {
            if (newChannel.Keyblock == oldChannel.Keyblock) return;
            await Task.Delay(1000);
        }
    }
}
