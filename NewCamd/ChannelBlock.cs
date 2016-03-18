using System;
using System.Linq;

namespace NewCamd
{
    class ChannelBlock
    {
        public int Channel { get; private set; }
        public DateTime From { get; private set; }
        public DateTime To { get; private set; }
        public byte[] Key { get; private set; }

        public static ChannelBlock Parse(int channel, byte[] data)
        {
            var cb = new ChannelBlock
            {
                Channel = channel,
                From = GetTimeStamp(data.Skip(16).Take(12).ToArray()),
                To = GetTimeStamp(data.Skip(32).Take(12).ToArray()),
                Key = data.Take(16).ToArray()
            };
            return cb;
        }

        private static DateTime GetTimeStamp(byte[] data)
        {
            var year = (data[1] << 8) + data[0];
            var month = data[2];
            var day = data[4];
            var hour = data[6];
            var min = data[8];
            var sec = data[10];
            return new DateTime(year,month,day,hour,min,sec);
        }
    }
}