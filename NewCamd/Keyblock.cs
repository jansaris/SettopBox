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
        byte[] _data;
        int _current;
        int _blocksize = 108;

        List<ChannelBlock> _blocks = new List<ChannelBlock>(); 

        public Keyblock(ILog logger, EncryptionHelpers crypto)
        {
            _logger = logger;
            _crypto = crypto;
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
            _data = File.ReadAllBytes("keyblock.dat");
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
            if (_data.Length < (_current + _blocksize))
            {
                return null;
            }
            var block = _data.Skip(_current).Take(_blocksize).ToArray();
            _current += _blocksize;
            return block;
        }

        public byte[] Decrypt(byte[] data)
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
            if (!Encoding.ASCII.GetString(block1).StartsWith("CEB"))
            {
                _logger.Warn($"Failed to decrypt block for channel {channel}");
                return null;
            }

            _logger.Debug($"Decryption of channel {channel} succeeded");
            return block1.Concat(block2).Concat(block3).ToArray();
            //Offset 32
            //OFFSET_CWKEYS 33
            /*
                                             response              request
            keyblock_analyse_file(unsigned char * dcw, unsigned char * ECM) {
            
            LOG(VERBOSE, "[KEYBLOCK] AES Key %2x %2x %2x %2x %2x %2x", mkey[0], mkey[1], mkey[2], mkey[3], mkey[4], mkey[5]);
			AES_set_decrypt_key(mkey, 128, &aesmkey);

			for (t = 0; t < 48; t += 16) {
				AES_ecb_encrypt(&ECM[24 + t], &ECM[24 + t], &aesmkey, AES_DECRYPT);
				LOG(VERBOSE, "[KEYBLOCK] DEC %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x", ECM[24 + t], ECM[24 + t +1], ECM[24 + t +2], ECM[24 + t +3], ECM[24 + t +4], ECM[24 + t +5], ECM[24 + t +6], ECM[24 + t +7], ECM[24 + t +8], ECM[24 + t +9], ECM[24 + t +10], ECM[24 + t +11], ECM[24 + t +12], ECM[24 + t +13], ECM[24 + t +14], ECM[24 + t +15]);
			}
			
			LOG(VERBOSE, "[KEYBLOCK] ECM %2x %2x %2x %2x %2x %2x", ECM[0], ECM[1], ECM[2], ECM[3], ECM[4], ECM[5]);
			LOG(VERBOSE, "[KEYBLOCK] Key 1 %2x %2x %2x %2x %2x %2x", ECM[0+OFFSET_CWKEYS], ECM[1+OFFSET_CWKEYS], ECM[2+OFFSET_CWKEYS], ECM[3+OFFSET_CWKEYS], ECM[4+OFFSET_CWKEYS], ECM[5+OFFSET_CWKEYS]);
			LOG(VERBOSE, "[KEYBLOCK] Key 2 %2x %2x %2x %2x %2x %2x", ECM[0+OFFSET_CWKEYS+16], ECM[1+OFFSET_CWKEYS+16], ECM[2+OFFSET_CWKEYS+16], ECM[3+OFFSET_CWKEYS+16], ECM[4+OFFSET_CWKEYS+16], ECM[5+OFFSET_CWKEYS+16]);

			
			if (memcmp(&ECM[24], "CEB", 3) == 0) {
				LOG(DEBUG, "[KEYBLOCK] ECM decrypt check passed");
			}
            */
        }

        public byte[] DecryptBlock(byte[] data)
        {
            File.WriteAllBytes("ChannelBlockRequest.dat", data);
            return Decrypt(data);
        }

        public void DecryptTest()
        {
            var bytes = File.ReadAllBytes("ChannelBlockRequest.dat");
            var data = Decrypt(bytes);
        }

        /*
        int32_t keyblock_analyse_file(unsigned char * dcw, unsigned char * ECM) {
        FILE *fp;
	unsigned char token[108];
	unsigned char * mkey;
	uint32_t t = 0;
	AES_KEY aesmkey;
	unsigned char table = ECM[0];
	uint16_t channel = (ECM[18] << 8) + ECM[19];
	time_t time_now, time_mkey1, time_mkey2;
	char valid_till_str[64];
	char valid_till_str2[64];
	fp = fopen(f_keyblock, "r");
	if (!fp) {
		LOG(ERROR, "[KEYBLOCK] Could not open file %s", f_keyblock);
		return (0);
	}
	LOG(INFO, "[KEYBLOCK] Find control word for Channel %d table 0x%02X", channel, table);

	fseek(fp, 4, SEEK_SET);
	while (fread(token, 108, 1, fp)) {
		if ((uint16_t) ((token[t + 1] << 8) + token[t]) == channel) {
			time_now = time(NULL);
			time_mkey1 = parse_ts(token + OFFSET_EXPIRE_MKEY1);
			time_mkey2 = parse_ts(token + OFFSET_EXPIRE_MKEY2);
			LOG(DEBUG, "[KEYBLOCK] Master keys found for Channel: %d. Valid till: %s - %s",	channel, ctime_r(&time_mkey1, valid_till_str), ctime_r(&time_now, valid_till_str2));

			if (difftime(time_mkey1, time_now) > 0) { // Check expire date mkey 1
				LOG(DEBUG, "[KEYBLOCK] Master key 1 selected");
				mkey = token + OFFSET_MKEY1;
			} else {
				if (difftime(time_mkey2, time_now) > 0) { // Check expire date mkey 2
					LOG(DEBUG, "[KEYBLOCK] Master key 2 selected");
					if (difftime(time_mkey2, time_now) < 86400) {
						LOG(DEBUG, "[KEYBLOCK] Warning: Master keys for Channel: %d will expire in %d minutes",	channel, (int)difftime(time_mkey2, time_now) / 60);
					}
					mkey = token + OFFSET_MKEY2;
				} else {
					LOG(INFO, "[KEYBLOCK] Keyblock is to old\n");
					return 0;
				}
			}
			LOG(VERBOSE, "[KEYBLOCK] AES Key %2x %2x %2x %2x %2x %2x", mkey[0], mkey[1], mkey[2], mkey[3], mkey[4], mkey[5]);
			AES_set_decrypt_key(mkey, 128, &aesmkey);

			for (t = 0; t < 48; t += 16) {
				AES_ecb_encrypt(&ECM[24 + t], &ECM[24 + t], &aesmkey, AES_DECRYPT);
				LOG(VERBOSE, "[KEYBLOCK] DEC %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x %2x", ECM[24 + t], ECM[24 + t +1], ECM[24 + t +2], ECM[24 + t +3], ECM[24 + t +4], ECM[24 + t +5], ECM[24 + t +6], ECM[24 + t +7], ECM[24 + t +8], ECM[24 + t +9], ECM[24 + t +10], ECM[24 + t +11], ECM[24 + t +12], ECM[24 + t +13], ECM[24 + t +14], ECM[24 + t +15]);
			}
			
			LOG(VERBOSE, "[KEYBLOCK] ECM %2x %2x %2x %2x %2x %2x", ECM[0], ECM[1], ECM[2], ECM[3], ECM[4], ECM[5]);
			LOG(VERBOSE, "[KEYBLOCK] Key 1 %2x %2x %2x %2x %2x %2x", ECM[0+OFFSET_CWKEYS], ECM[1+OFFSET_CWKEYS], ECM[2+OFFSET_CWKEYS], ECM[3+OFFSET_CWKEYS], ECM[4+OFFSET_CWKEYS], ECM[5+OFFSET_CWKEYS]);
			LOG(VERBOSE, "[KEYBLOCK] Key 2 %2x %2x %2x %2x %2x %2x", ECM[0+OFFSET_CWKEYS+16], ECM[1+OFFSET_CWKEYS+16], ECM[2+OFFSET_CWKEYS+16], ECM[3+OFFSET_CWKEYS+16], ECM[4+OFFSET_CWKEYS+16], ECM[5+OFFSET_CWKEYS+16]);

			
			if (memcmp(&ECM[24], "CEB", 3) == 0) {
				LOG(DEBUG, "[KEYBLOCK] ECM decrypt check passed");
			} else {
				LOG(VERBOSE, "[KEYBLOCK] Check %2x %2x %2x", ECM[24], ECM[25], ECM[26]);
				LOG(ERROR, "[KEYBLOCK] ECM decrypt failed, wrong master key or unknown format");
				//fclose(fp);
				//return 0;
			}
			if (table == 0x80) {
				memcpy(dcw, ECM + OFFSET_CWKEYS, 32);
			} else {
				memcpy(dcw, ECM + OFFSET_CWKEYS + 16, 16);
				memcpy(dcw + 16, ECM + OFFSET_CWKEYS, 16);
			}
			fclose(fp);
			return 1;
		}
	}
	LOG(ERROR, "[KEYBLOCK] No Master key found for channel: %d, cannot decrypt ECM", channel);
	fclose(fp);
	return 0;
        */
    }
}