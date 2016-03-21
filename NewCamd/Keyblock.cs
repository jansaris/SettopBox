using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using NewCamd.Encryption;

namespace NewCamd
{
    public class Keyblock
    {
        readonly ILog _logger;
        readonly EncryptionHelpers _crypto;
        readonly Settings _settings;

        byte[] _data;
        int _current;
        const int Blocksize = 108;

        List<ChannelBlock> _blocks = new List<ChannelBlock>(); 

        public Keyblock(ILog logger, EncryptionHelpers crypto, Settings settings)
        {
            _logger = logger;
            _crypto = crypto;
            _settings = settings;
        }

        public void Prepare()
        {
            _logger.Debug("Start splitting keyblocks");
            ReadKeyBlock();
            SplitKeyBlock();
            _logger.Debug($"Parsed {_blocks.Count} channel blocks");
        }

        void ReadKeyBlock()
        {
            _data = File.ReadAllBytes(Path.Combine(_settings.DataFolder, "keyblock.dat"));
        }

        void SplitKeyBlock()
        {
            _current = 4;
            var blocks = new List<ChannelBlock>();
            var block = NextBlock();
            while (block != null)
            {
                var channel = (block[1] << 8) + block[0];
                blocks.Add(ChannelBlock.Parse(channel, block.Skip(4).Take(52).ToArray()));
                blocks.Add(ChannelBlock.Parse(channel, block.Skip(56).Take(52).ToArray()));
                block = NextBlock();
            }
            _blocks = blocks;
        }

        byte[] NextBlock()
        {
            if (_data.Length < (_current + Blocksize))
            {
                return null;
            }
            var block = _data.Skip(_current).Take(Blocksize).ToArray();
            _current += Blocksize;
            return block;
        }

        public byte[] DecryptBlock(byte[] data)
        {
            var channel = (data[18] << 8) + data[19];
            var channelBlock = _blocks.FirstOrDefault(b => b.Channel == channel && b.From < DateTime.Now && b.To > DateTime.Now);
            if (channelBlock == null)
            {
                _logger.Warn($"No valid block found for channel {channel}");
                return null;
            }
            var block1 = _crypto.AesDecrypt(data.Skip(24).Take(16).ToArray(), channelBlock.Key);
            var block2 = _crypto.AesDecrypt(data.Skip(40).Take(16).ToArray(), channelBlock.Key);
            var block3 = _crypto.AesDecrypt(data.Skip(56).Take(16).ToArray(), channelBlock.Key);
            if (!Encoding.ASCII.GetString(block1).StartsWith("CEB", StringComparison.Ordinal))
            {
                _logger.Warn($"Failed to decrypt block for channel {channel}");
                return null;
            }

            _logger.Debug($"Decryption of channel {channel} succeeded");
            return block1.Concat(block2).Concat(block3).ToArray();
        }
    }
}