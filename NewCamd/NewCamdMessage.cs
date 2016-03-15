namespace NewCamd
{
    public class NewCamdMessage
    {
        public const int HeaderLength = 8;
        public const int Size = 400;
        public NewCamdMessageType Type { get; set; }
        public int MessageId { get; set; }
        public int ServiceId { get; set; }
        public int ProviderId { get; set; }
        public byte[] Data { get; set; }
    }
}