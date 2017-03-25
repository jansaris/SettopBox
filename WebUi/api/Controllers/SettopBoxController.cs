using log4net;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;
using WebUi.api.Models;
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
        readonly IptvChannel _iptvChannel;
        static List<Channel> _channels;

        public SettopBoxController(ILog logger, ModuleCommunication info, IptvChannel iptvChannel)
        {
            _logger = logger;
            _info = info;
            _iptvChannel = iptvChannel;
        }

        public IHttpActionResult Get()
        {
            _logger.Info("get all channels");
            LoadChannels(true);
            return Ok(_channels);
        }

        private void LoadChannels(bool force)
        {
            //if(force) LoadDummyData(); return;
            
            if (_channels != null && !force) return;
            var channels = _info.Data(nameof(ChannelList)) as ChannelListInfo;
            if (channels == null)
            {
                _channels = new List<Channel>();
                return;
            }
            _channels = channels.Channels.Select(Convert).ToList();

            if (_info.Data(nameof(EpgGrabber)) is EpgGrabberInfo epg)
            {
                _channels.ForEach(c =>
                {
                    if (epg.Channels.Contains(c.Name)) c.EpgGrabber = true;
                });
            }
            if (_info.Data(nameof(Keyblock)) is KeyblockInfo keyblock)
            {
                _channels.ForEach(c =>
                {
                    if (keyblock.ChannelsToMonitor.ContainsKey(c.Id))
                    {
                        c.Keyblock = true;
                        c.KeyblockId = keyblock.ChannelsToMonitor[c.Id];
                    }
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
            var channel = _channels.FirstOrDefault(c => c.Id == id);
            if (channel == null) return NotFound();
            return Ok(channel);
        }

        [Route("iptvInfo/{id}")]
        [HttpGet]
        public IHttpActionResult IptvInfo(string id)
        {
            _logger.Info($"get channels Iptv info: {id}");
            LoadChannels(false);
            var channel = _channels.FirstOrDefault(c => c.Id == id);
            if (channel == null) return NotFound();
            var data = channel.AvailableChannels
                //.AsParallel()
                .Select(c => _iptvChannel.ReadInfo(c.Url, c.Name))
                .Where(inf => inf != null)
                .ToList();
            return Ok(data);
        }

        public IHttpActionResult Put(Channel channel)
        {
            try
            {
                _logger.Info($"Update channel: {channel?.Id}");
                LoadChannels(false);
                var index = _channels.FindIndex(c => c.Id == channel.Id);
                if (index == -1) return NotFound();
                UpdateChannel(channel, _channels[index]);
                _channels[index] = channel;
                return Ok();
            }
            catch(Exception ex)
            {
                _logger.Error($"Failed to update {channel?.Id}: {ex.Message}", ex);
                return InternalServerError(ex);
            }
        }

        private void UpdateChannel(Channel newChannel, Channel oldChannel)
        {
            UpdateKeyblock(newChannel, oldChannel);
            UpdateEpg(newChannel, oldChannel);
            UpdateTvHeadend(newChannel, oldChannel);
        }

        private void UpdateTvHeadend(Channel newChannel, Channel oldChannel)
        {
            if (newChannel.TvHeadend == oldChannel.TvHeadend &&
                newChannel.TvHeadendChannel == oldChannel.TvHeadendChannel &&
                newChannel.EpgGrabber == oldChannel.EpgGrabber) return;
            if (newChannel.TvHeadend && !oldChannel.TvHeadend) _logger.Info($"Add to TvHeadend: {newChannel.Id} - {newChannel.Name}");
            else if (newChannel.TvHeadend == oldChannel.TvHeadend) _logger.Info($"Update TvHeadend: {newChannel.Id} to {newChannel.Name}");
            else if (!newChannel.TvHeadend && oldChannel.TvHeadend) _logger.Info($"Remove from TvHeadend: {oldChannel.Id}");

            var tcu = new TvHeadendChannelUpdate { Id = newChannel.Id, OldUrl = oldChannel.TvHeadendChannel, NewUrl = newChannel.TvHeadendChannel, Epg = newChannel.EpgGrabber };
            var data = new CommunicationData(DataType.TvHeadendChannelUpdate, tcu);
            var thread = _info.SendData(nameof(WebUi), nameof(TvHeadendIntegration), data);
            thread?.Join();
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

        private void UpdateKeyblock(Channel newChannel, Channel oldChannel)
        {
            if (newChannel.Keyblock == oldChannel.Keyblock && newChannel.KeyblockId == oldChannel.KeyblockId) return;
            _logger.Info($"{(newChannel.Keyblock ? "Add to" : "Remove from")} Keyblock - {newChannel.Name}: {newChannel.KeyblockId}");
            var kcu = new KeyblockChannelUpdate { Id = newChannel.Id, Name = newChannel.Name, Enabled = newChannel.Keyblock, OldKey = oldChannel.KeyblockId, NewKey = newChannel.KeyblockId };
            var data = new CommunicationData(DataType.KeyblockChannelUpdate, kcu);
            var thread = _info.SendData(nameof(WebUi), nameof(Keyblock), data);
            thread?.Join();
        }

        private void LoadDummyData()
        {
            _channels = new List<Channel>
            {
                new Channel
                {
                    Id = "ned1",
                    Number = 1,
                    Name = "NPO 1",
                   AvailableChannels = new List<ChannelLocation>
                   {
                       new ChannelLocation{ Name= "HD+", Url="igmp://224.124.25.128:8426" },
                       new ChannelLocation{ Name= "SD",Url="igmp://239.115.38.221:5689" },
                       new ChannelLocation{ Name= "",Url="igmp://224.24.125.12:3421" },
                   },
                   EpgGrabber = true,
                   Keyblock = false,
                   KeyblockId = 661,
                   TvHeadend = true,
                   TvHeadendChannel = "igmp://224.124.25.128:8426"

                },
                new Channel
                {
                    Id = "ned2",
                    Number = 2,
                    Name = "NPO 2",
                   AvailableChannels = new List<ChannelLocation>
                   {
                       new ChannelLocation{ Name= "HD+", Url="gmp://224.124.25.128:8426" },
                       new ChannelLocation{ Name= "HD", Url="gmp://239.115.38.221:5689" },
                       new ChannelLocation{ Name= "SD", Url="igmp://224.24.125.12:3421" },
                   },
                   EpgGrabber = true,
                   Keyblock = true,
                   KeyblockId = 662,
                   TvHeadend = true,
                   TvHeadendChannel = "igmp://224.124.25.128:8426"

                },
                new Channel
                {
                    Id = "ned3",
                    Number = 3,
                    Name = "NPO 3",
                   AvailableChannels = new List<ChannelLocation>
                   {
                       new ChannelLocation{ Name= "HD+", Url="igmp://224.124.25.128:8426" },
                       new ChannelLocation{ Name= "", Url="igmp://239.115.38.221:5689" },
                       new ChannelLocation{ Name= "", Url="igmp://224.24.125.12:3421" },
                   },
                   EpgGrabber = false,
                   Keyblock = false,
                   TvHeadend = false
                }
            };
        }
    }
}
