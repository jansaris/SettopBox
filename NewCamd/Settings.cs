using System;
using System.Collections.Generic;
using SharedComponents.Settings;

namespace NewCamd
{
    public class Settings : IniSettings
    {
        protected override string Name => "NewCamd";
        public string Username { get; private set; } = "user";
        public string Password { get; private set; } = "pass";
        public string IpAdress { get; set; } = String.Empty;
        public int Port { get; private set; } = 15050;
        public string DesKey { get; private set; } = "0102030405060708091011121314";
        public string DataFolder { get; private set; } = "Data";
        public string KeyblockFile { get; private set; } = "Keyblock.dat";

        public byte[] GetDesArray()
        {
            var retValue = new List<byte>();
            var chars = DesKey.ToCharArray();
            for (var i = 0; i + 1 < chars.Length; i += 2)
            {
                var hex = "" + chars[i] + chars[i + 1];
                retValue.Add((byte)int.Parse(hex, System.Globalization.NumberStyles.HexNumber));
            }
            return retValue.ToArray();
        } 

        public int MaxWaitTimeInMs { get; private set; } = 60000;
    }
}