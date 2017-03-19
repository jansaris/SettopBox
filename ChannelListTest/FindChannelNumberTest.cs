using FakeItEasy;
using FluentAssertions;
using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelListTest
{
    [TestFixture]
    class FindChannelNumberTest
    {
        ILog logger;

        public FindChannelNumberTest()
        {
            logger = A.Fake<ILog>();
            A.CallTo(() => logger.Info(A<string>._)).Invokes(Console.WriteLine);
            A.CallTo(() => logger.Debug(A<string>._)).Invokes(Console.WriteLine);
        }

        string file = @"C:\Users\Jan\Documents\Git\SettopBox\SettopBox\bin\Debug\UdpPart";
        string GetFile(int part)
        {
            return file + part + ".data";
        }

        [Test]
        public void GenerateTestFile()
        {
            using (var writer = new StreamWriter(File.OpenWrite("c:\\temp\\test.data")))
            {
                var data = StringToByteArray("20 20 20 01 ff 20 01 fc 80 17 48 15 19 04 4b 50 4e 20 0e 4e 50 4f 20 31 20 48 44 20 67 6c 61 73 20 7d 74 56 42 ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff");
                writer.Write(data);
            }
        }

        //[Test]
        //public void FindChannel661()
        //{
        //    var bytes = File.ReadAllBytes("C:\\temp\\Stream.data");
        //    var result = FindChannelNumber(bytes, out int channel);
        //    result.Should().BeTrue();
        //    channel.Should().Be(661);
        //}

        //[Test]
        //public void FindChannelNPO_1_glas()
        //{
        //    var bytes = File.ReadAllBytes("C:\\temp\\Stream.data");
        //    var result = FindChannelInfo(bytes, out string provider, out string channel);
        //    result.Should().BeTrue();
        //    provider.Should().Be("KPN");
        //    channel.Should().Be("NPO 1 HD glas");
        //}
        
        public static byte[] StringToByteArray(string hex)
        {
            if (hex.Contains(" ")) hex = hex.Replace(" ", "");
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        [Test]
        public void FindNpo2()
        {
            var logger = A.Fake<ILog>();
            A.CallTo(() => logger.Info(A<string>._)).Invokes(Console.WriteLine);
            var channelInfo = new WebUi.api.Rtp.ChannelTester(logger);
            var info = channelInfo.ReadInfo("igmp://224.0.251.125:8250");
            info.Number.Should().Be(662);
            info.Provider.Should().Be("KPN");
            info.Name.Should().Be("NPO 2 HD glas");
        }
    }
}
