using log4net;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;
using WebUi.api.Models;
using System;
using System.Threading.Tasks;

namespace WebUi.api.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/settopbox")]
    public class SettopBoxController : ApiController
    {
        private ILog _logger;
        private List<Channel> _channels;

        public SettopBoxController(ILog logger)
        {
            _logger = logger;
            GenerateChannels();
        }

        private void GenerateChannels()
        {
            _channels = new List<Channel>
            {
                new Channel
                {
                    Id = "ned1",
                    Number = 1,
                    Name = "NPO 1",
                   AvailableChannels = new List<Tuple<string,string>>
                   {
                       new Tuple<string,string>("HD+", "igmp://224.124.25.128:8426"),
                       new Tuple<string,string>("SD","igmp://239.115.38.221:5689"),
                       new Tuple<string,string>("","igmp://224.24.125.12:3421"),
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
                   AvailableChannels = new List<Tuple<string,string>>
                   {
                       new Tuple<string,string>("HD+","igmp://224.124.25.128:8426"),
                       new Tuple<string,string>("HD","igmp://239.115.38.221:5689"),
                       new Tuple<string,string>("SD","igmp://224.24.125.12:3421"),
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
                   AvailableChannels = new List<Tuple<string,string>>
                   {
                       new Tuple<string,string>("HD+","igmp://224.124.25.128:8426"),
                       new Tuple<string,string>("","igmp://239.115.38.221:5689"),
                       new Tuple<string,string>("","igmp://224.24.125.12:3421"),
                   },
                   EpgGrabber = false,
                   Keyblock = false,
                   TvHeadend = false
                }
            };
        }

        public IHttpActionResult Get()
        {
            return Ok(_channels);
        }

        public IHttpActionResult Get(string id)
        {
            var index = _channels.FindIndex(c => c.Id == id);
            if (index == -1) return NotFound();
            return Ok(_channels[index]);
        }

        public async Task<IHttpActionResult> Put(Channel channel)
        {
            var index = _channels.FindIndex(c => c.Id == channel.Id);
            if (index == -1) return NotFound();
            await UpdateChannel(channel, _channels[index]);
            return Ok();
        }

        private async Task UpdateChannel(Channel newChannel, Channel oldChannel)
        {
            await UpdateKeyblock(newChannel, oldChannel);
            await UpdateEpg(newChannel, oldChannel);
            await UpdateTvHeadend(newChannel, oldChannel);
        }

        private async Task UpdateTvHeadend(Channel newChannel, Channel oldChannel)
        {
            if(newChannel.TvHeadend != oldChannel.TvHeadend)
            {
                await Task.Delay(1000);
            }
            else if(newChannel.TvHeadendChannel != oldChannel.TvHeadendChannel)
            {
                await Task.Delay(500);
            }
        }

        private async Task UpdateEpg(Channel newChannel, Channel oldChannel)
        {
            if (newChannel.EpgGrabber == oldChannel.EpgGrabber) return;
            await Task.Delay(1000);
        }

        private async Task UpdateKeyblock(Channel newChannel, Channel oldChannel)
        {
            if (newChannel.Keyblock == oldChannel.Keyblock) return;
            await Task.Delay(1000);
        }
    }
}
