using log4net;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;
using WebUi.api.Models;
using SharedComponents.Module;
using System.Linq;
using SharedComponents.Models;
using System;

namespace WebUi.api.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/settopbox")]
    public class SettopBoxController : ApiController
    {
        readonly ILog _logger;
        readonly ModuleCommunication _info;
        static List<Channel> _channels;

        public SettopBoxController(ILog logger, ModuleCommunication info)
        {
            _logger = logger;
            _info = info;
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
            if (!(_info.Data(nameof(ChannelList)) is ChannelListInfo channels))
            {
                //Failed to retrieve channellist from module
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
                _channels.ForEach(c => c.AvailableChannels.ForEach(location => 
                {
                    location.Keyblock = keyblock.AvailableKeyblockIds.Contains(location.KeyblockId);
                }));
            }

            if(_info.Data(nameof(TvHeadendIntegration)) is TvHeadendIntegrationInfo tvh)
            {
                _channels.ForEach(c =>
                {
                    var match = tvh.Channels.FirstOrDefault(tvhChannel => c.AvailableChannels.Any(l => l.Url == tvhChannel.Url));
                    if (match != null)
                    {
                        c.TvHeadend = true;
                        c.TvHeadendChannel = match.Url;
                        c.TvhId = match.UUID;
                    }
                });
            }
        }

        private Channel Convert(ChannelInfo info)
        {
            return new Channel
            {
                Name = info.Name,
                Names = info.Names,
                Id = Guid.NewGuid().ToString(),
                Number = info.Number,
                AvailableChannels = info.Locations.ToList()
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
                .Select(c => new IptvInfo
                {
                    Name = GetName(c.Bitrate),
                    Url = c.Url,
                    Number = c.KeyblockId,
                    Keyblock = c.Keyblock,
                    Provider = "KPN",
                    KBps = c.Bitrate,
                    MBps = c.Bitrate / 1024
                })
                .ToList();
            return Ok(data);
        }

        private string GetName(int bitrate)
        {
            if (bitrate > 8000) return "Glas";
            if (bitrate > 4000) return "HD";
            if (bitrate > 2000) return "SD";
            return "Radio";
        }

        public IHttpActionResult Put(Channel channel)
        {
            try
            {
                if (channel == null) return NotFound();
                _logger.Info($"Update channel: {channel.Name}");
                LoadChannels(false);
                var index = _channels.FindIndex(c => c.Id == channel.Id);
                if (index == -1) return NotFound();
                UpdateChannel(channel, _channels[index]);
                _channels[index] = channel;
                return Ok();
            }
            catch(Exception ex)
            {
                _logger.Error($"Failed to update {channel?.Name}: {ex.Message}", ex);
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
            if (newChannel.TvHeadend && !oldChannel.TvHeadend) _logger.Info($"Add to TvHeadend: {newChannel.Number} - {newChannel.Name}");
            else if (newChannel.TvHeadend == oldChannel.TvHeadend) _logger.Info($"Update TvHeadend: {newChannel.Number} to {newChannel.Name}");
            else if (!newChannel.TvHeadend && oldChannel.TvHeadend) _logger.Info($"Remove from TvHeadend: {oldChannel.Name}");

            var tcu = new TvHeadendChannelUpdate { TvhId = newChannel.TvhId, Id = newChannel.Number.ToString(), OldUrl = oldChannel.TvHeadendChannel, NewUrl = newChannel.TvHeadendChannel, Epg = newChannel.EpgGrabber };

            SendData(nameof(TvHeadendIntegration), DataType.TvHeadendChannelUpdate, tcu);
        }

        private void UpdateEpg(Channel newChannel, Channel oldChannel)
        {
            if (newChannel.EpgGrabber == oldChannel.EpgGrabber) return;
            foreach (var name in newChannel.Names)
            {
                _logger.Info($"{(newChannel.EpgGrabber ? "Add to" : "Remove from")} EpgGrabber - {name}");
                var ecu = new EpgChannelUpdate { Id = newChannel.Number.ToString(), Name = name, Enabled = newChannel.EpgGrabber };
                SendData(nameof(EpgGrabber), DataType.EpgChannelUpdate, ecu);
            }
        }

        private void UpdateKeyblock(Channel newChannel, Channel oldChannel)
        {
            if (newChannel.Keyblock == oldChannel.Keyblock && newChannel.KeyblockId == oldChannel.KeyblockId) return;
            _logger.Info($"{(newChannel.Keyblock ? "Add to" : "Remove from")} Keyblock - {newChannel.Name}: {newChannel.KeyblockId}");
            var kcu = new KeyblockChannelUpdate { Id = newChannel.Number.ToString(), Name = newChannel.Name, Enabled = newChannel.Keyblock, OldKey = oldChannel.KeyblockId, NewKey = newChannel.KeyblockId };
            
            SendData(nameof(Keyblock), DataType.KeyblockChannelUpdate, kcu);
        }

        private void SendData(string module, DataType type, object data)
        {
            var communicationData = new CommunicationData(type, data);
            var thread = _info.SendData(nameof(WebUi), module, communicationData);
            thread?.Join();
        }

        //private void LoadDummyData()
        //{
        //    _channels = new List<Channel>
        //    {
        //        new Channel
        //        {
        //            Id = "ned1",
        //            Number = 1,
        //            Name = "NPO 1",
        //           AvailableChannels = new List<ChannelLocation>
        //           {
        //               new ChannelLocation {
        //                    Bitrate = 12750,
        //                    Protocol = "igmp",
        //                    Host = "224.124.25.128",
        //                    Port = 8426 },
        //               new ChannelLocation {
        //                   Bitrate = 3300,
        //                   Protocol = "igmp",
        //                   Host = "239.115.38.221",
        //                   Port = 5689 },
        //               new ChannelLocation {
        //                   Bitrate= 7700,
        //                   Protocol = "igmp",
        //                   Host = "224.24.125.12",
        //                   Port = 3421
        //               }
        //           },
        //           EpgGrabber = true,
        //           Keyblock = false,
        //           KeyblockId = 661,
        //           TvHeadend = true,
        //           TvHeadendChannel = "igmp://224.124.25.128:8426"

        //        },
        //        new Channel
        //        {
        //            Id = "ned2",
        //            Number = 2,
        //            Name = "NPO 2",
        //           AvailableChannels = new List<ChannelLocation>
        //           {
        //               new ChannelLocation {
        //                   Bitrate = 12750,
        //                   Protocol = "igmp",
        //                   Host = "224.124.25.128",
        //                   Port = 8426 },
        //               new ChannelLocation {
        //                   Bitrate = 3300,
        //                   Protocol = "igmp",
        //                   Host = "239.115.38.221",
        //                   Port = 5689 },
        //               new ChannelLocation {
        //                   Bitrate= 7700,
        //                   Protocol = "igmp",
        //                   Host = "224.24.125.12",
        //                   Port = 3421
        //               }
        //           },
        //           EpgGrabber = true,
        //           Keyblock = true,
        //           KeyblockId = 662,
        //           TvHeadend = true,
        //           TvHeadendChannel = "igmp://224.124.25.128:8426"

        //        },
        //        new Channel
        //        {
        //            Id = "ned3",
        //            Number = 3,
        //            Name = "NPO 3",
        //           AvailableChannels = new List<ChannelLocation>
        //           {
        //               new ChannelLocation {
        //                   Bitrate = 7700,
        //                   Protocol = "igmp",
        //                   Host = "224.124.25.128",
        //                   Port = 8426 },
        //               new ChannelLocation {
        //                   Bitrate = 3300,
        //                   Protocol = "igmp",
        //                   Host = "239.115.38.221",
        //                   Port = 5689 },
        //               new ChannelLocation {
        //                   Bitrate= 3300,
        //                   Protocol = "igmp",
        //                   Host = "224.24.125.12",
        //                   Port = 3421 
        //               }
        //           },
        //           EpgGrabber = false,
        //           Keyblock = false,
        //           TvHeadend = false
        //        }
        //    };
        //}
    }
}
