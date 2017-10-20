using ChannelList;
using NUnit.Framework;
using FluentAssertions;

namespace ChannelListTest
{
    [TestFixture]
    public class RtspDataReceiverTest
    {
        [Test]
        public void TestConnection()
        {
            var receiver = new RtspDataReceiver();
            var data = receiver.ReadDataFromServer("213.75.116.138", 8554);
            data.Length.Should().BeGreaterThan(0);
        }
    }
}
