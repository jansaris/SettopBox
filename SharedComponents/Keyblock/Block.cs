using System;
using System.Collections.Generic;
using System.Linq;
using log4net;

namespace SharedComponents.Keyblock
{
    public class Block
    {
        readonly ILog _logger;
        const int Blocksize = 108;

        List<Channel> _blocks = new List<Channel>();
        DateTime _refreshDate;
        private IEnumerable<int> _refreshDateChannelIds;

        public Block(ILog logger)
        {
            _logger = logger;
        }

        public void Load(byte[] data, IList<int> channelsToMonitor, IList<int> channelsToIgnore)
        {
            if (data == null)
            {
                _logger.Warn("No data received to split");
                return;
            }

            _logger.Debug("Start splitting keyblocks");
            _blocks = SplitKeyBlock(data);
            _logger.Debug($"Parsed {_blocks.Count} channel blocks");
                
            var grouped = GetChannelDictionary(channelsToMonitor, channelsToIgnore);
            //Take the minimal last date per block 
            //often we got 2 blocks per channel, 1 block per week
            //So take always the last block as reference point for refresh
            _refreshDate = grouped.Min(g => g.Value.Last().To);
            //And save the ID's of the channels for logging purposes
            _refreshDateChannelIds = grouped.Where(g => g.Value.Last().To == _refreshDate)
                                            .Select(k => k.Key)
                                            .ToList();

            foreach (var keyvalue in grouped)
            {
                _logger.Debug($"Channel {keyvalue.Key}: {keyvalue.Value.Count} blocks, valid between: {keyvalue.Value.First().From} - {keyvalue.Value.Last().To}");
            }
        }

        private Dictionary<int, List<Channel>> GetChannelDictionary(IList<int> channelsToMonitor, IList<int> channelsToIgnore)
        {
            if (channelsToMonitor == null) channelsToMonitor = new List<int>();
            if (channelsToIgnore == null) channelsToIgnore = new List<int>();

            //Group all the blocks by ChannelId and
            var grouped = _blocks.GroupBy(c => c.ChannelId);

            if(channelsToMonitor.Any())
            {
                _logger.Debug($"Filter {channelsToIgnore.Count} channels to monitor from timestamp validation: {string.Join(";", channelsToMonitor)}");
                grouped = grouped.Where(c => channelsToMonitor.Contains(c.Key));
            }

            if (channelsToIgnore.Any())
            {
                _logger.Debug($"Skip {channelsToIgnore.Count} channels from timestamp validation: {string.Join(";", channelsToIgnore)}");
                grouped = grouped.Where(c => !channelsToIgnore.Contains(c.Key));
            }

            //And order the blocks within the channel by date
            return grouped.ToDictionary(c => c.Key, c => c.OrderBy(ch => ch.From).ToList());
        }

        public int NrOfChannels => _blocks.Count;
        public DateTime ValidFrom => _blocks.Count > 0 ? _blocks.Min(c => c.From) : DateTime.MinValue;
        public DateTime ValidTo => _blocks.Count > 0 ? _blocks.Max(c => c.To) : DateTime.MinValue;
        public DateTime NeedsRefreshAfter => _refreshDate;
        public IEnumerable<int> NeedsRefreshAfterChannelIds => _refreshDateChannelIds;

        protected Channel GetChannelById(int channel)
        {
            return _blocks.FirstOrDefault(b => b.ChannelId == channel && b.From < DateTime.Now && b.To > DateTime.Now);
        }

        List<Channel> SplitKeyBlock(byte[] keyblock)
        {
            var index = 4;
            var blocks = new List<Channel>();

            var block = NextBlock(keyblock, index);
            while (block != null)
            {
                index += Blocksize;

                var channel = (block[1] << 8) + block[0];
                blocks.Add(Channel.Parse(channel, block.Skip(4).Take(52).ToArray()));
                blocks.Add(Channel.Parse(channel, block.Skip(56).Take(52).ToArray()));

                block = NextBlock(keyblock, index);
            }
            return blocks;
        }

        static byte[] NextBlock(byte[] keyblock, int index)
        {
            if (keyblock.Length < (index + Blocksize))
            {
                return null;
            }
            var block = keyblock.Skip(index).Take(Blocksize).ToArray();
            return block;
        }

        public DateTime? FirstFutureExpirationDate(IList<int> channelsToMonitor, IList<int> channelsToIgnore)
        {
            var grouped = GetChannelDictionary(channelsToMonitor, channelsToIgnore);
            //Take the minimal last date per block 
            //often we got 2 blocks per channel, 1 block per week
            //So take always the last block as reference point for refresh
            var filteredDates = grouped.Where(g => g.Value.Last().To > DateTime.Now)
                                       .Select(g => g.Value.Last().To)
                                       .ToList();

            if (filteredDates.Any())
            {
                var date = filteredDates.Min();
                _logger.Info($"Found first valid block in the future which is valid till {date}");
                return date;
            }
            _logger.Warn("Found NO valid expiration date between in blocks in the future");
            return null;
        }
    }
}
