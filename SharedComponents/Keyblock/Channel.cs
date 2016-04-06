using System;
using System.Linq;

namespace SharedComponents.Keyblock
{
    public class Channel
    {
        public int ChannelId { get; private set; }
        public DateTime From { get; private set; }
        public DateTime To { get; private set; }
        public byte[] Key { get; private set; }

        public static Channel Parse(int channel, byte[] data)
        {
            var cb = new Channel
            {
                ChannelId = channel,
                From = GetTimeStamp(data.Skip(16).Take(12).ToArray()),
                To = GetTimeStamp(data.Skip(32).Take(12).ToArray()),
                Key = data.Take(16).ToArray()
            };
            return cb;
        }

        static DateTime GetTimeStamp(byte[] data)
        {
            var year = BitConverter.ToInt16(data, 0);
            var month = BitConverter.ToInt16(data, 2);
            var day = BitConverter.ToInt16(data, 4);
            var hour = BitConverter.ToInt16(data, 6);
            var min = BitConverter.ToInt16(data, 8);
            var sec = BitConverter.ToInt16(data, 10);
            return new DateTime(year, month, day, hour, min, sec);
        }
    }
}