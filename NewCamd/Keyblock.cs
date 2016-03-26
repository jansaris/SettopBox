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
            var data = ReadKeyBlock();
            _blocks = SplitKeyBlock(data);
            _logger.Debug($"Parsed {_blocks.Count} channel blocks");
        }

        byte[] ReadKeyBlock()
        {
            return File.ReadAllBytes(Path.Combine(_settings.DataFolder, _settings.KeyblockFile));
        }

        List<ChannelBlock> SplitKeyBlock(byte[] keyblock)
        {
            var index = 4;
            var blocks = new List<ChannelBlock>();

            var block = NextBlock(keyblock, index);
            while (block != null)
            {
                index += Blocksize;

                var channel = (block[1] << 8) + block[0];
                blocks.Add(ChannelBlock.Parse(channel, block.Skip(4).Take(52).ToArray()));
                blocks.Add(ChannelBlock.Parse(channel, block.Skip(56).Take(52).ToArray()));

                block = NextBlock(keyblock, index);
            }
            return blocks;
        }

        byte[] NextBlock(byte[] keyblock, int index)
        {
            if (keyblock.Length < (index + Blocksize))
            {
                return null;
            }
            var block = keyblock.Skip(index).Take(Blocksize).ToArray();
            return block;
        }

        public byte[] DecryptBlock(NewCamdMessageType type, byte[] data)
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

            _logger.Info($"Decryption of channel {channel} succeeded");
            /*
            #define OFFSET_CWKEYS 33
            AES_ecb_encrypt(&ECM[24 + t], &ECM[24 + t], &aesmkey, AES_DECRYPT);
            if (table == 0x80) {
				memcpy(dcw, ECM + OFFSET_CWKEYS, 32);
			} else {
				memcpy(dcw, ECM + OFFSET_CWKEYS + 16, 16);
				memcpy(dcw + 16, ECM + OFFSET_CWKEYS, 16);
			}*/
            var key1 = block1.Skip(9).Take(7).Concat(block2).Concat(block3.Take(9));
            var key2 = block2.Skip(9).Take(7).Concat(block3.Take(9));

            return type == NewCamdMessageType.MsgKeyblockReq1 ? 
                key1.Concat(key2).ToArray() : 
                key2.Concat(key1).ToArray();
        }
    }
}