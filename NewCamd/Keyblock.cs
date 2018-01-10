using System;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using NewCamd.Encryption;

namespace NewCamd
{
    public class Keyblock : SharedComponents.Keyblock.Block
    {
        readonly ILog _logger;
        readonly EncryptionHelpers _crypto;
        readonly Settings _settings;

        public Keyblock(ILog logger, EncryptionHelpers crypto, Settings settings) : base(logger)
        {
            _logger = logger;
            _crypto = crypto;
            _settings = settings;
        }

        public void Prepare(string keyblockFile = null)
        {
            if (string.IsNullOrWhiteSpace(keyblockFile))
            {
                keyblockFile = Path.Combine(_settings.DataFolder, _settings.KeyblockFile);
            }
            Load(ReadKeyBlock(keyblockFile), null, null);
        }

        byte[] ReadKeyBlock(string file)
        {
            if (!File.Exists(file))
            {
                _logger.Warn($"Keyblock file '{file}' doesn't exists");
                return null;
            }
            _logger.Debug($"Read {file} from disk");
            return File.ReadAllBytes(Path.Combine(_settings.DataFolder, _settings.KeyblockFile));
        }

        public byte[] DecryptBlock(NewCamdMessageType type, byte[] data)
        {
            var channel = (data[18] << 8) + data[19];
            var channelBlock = GetChannelById(channel);
            if (channelBlock == null)
            {
                _logger.Warn($"No valid block found for channel {channel}");
                return new byte[0];
            }

            var block1 = _crypto.AesDecrypt(data.Skip(24).Take(16).ToArray(), channelBlock.Key);
            var block2 = _crypto.AesDecrypt(data.Skip(40).Take(16).ToArray(), channelBlock.Key);
            var block3 = _crypto.AesDecrypt(data.Skip(56).Take(16).ToArray(), channelBlock.Key);

            if (!Encoding.ASCII.GetString(block1).StartsWith("CEB", StringComparison.Ordinal))
            {
                _logger.Warn($"Failed to decrypt block for channel {channel}");
                return new byte[0];
            }

            _logger.Info($"Decryption of channel {channel} succeeded");
            var key1 = block1.Skip(9).Take(7).Concat(block2).Concat(block3.Take(9));
            var key2 = block2.Skip(9).Take(7).Concat(block3.Take(9));

            return type == NewCamdMessageType.MsgKeyblockReq1 ? 
                key1.Concat(key2).ToArray() : 
                key2.Concat(key1).ToArray();
        }
    }
}