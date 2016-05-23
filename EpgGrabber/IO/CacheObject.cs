using System;
using System.Globalization;
using System.IO;

namespace EpgGrabber.IO
{
    internal class CacheObject
    {
        public CacheObject()
        {
            Date = DateTime.Now;
        }

        private CacheObject(string url) : this()
        {
            Url = url;
        }

        public CacheObject(string url, string data) : this(url)
        {
            StringData = data;
            DataType = ObjectType.String;
        }

        public CacheObject(string url, byte[] data) : this(url)
        {
            ByteData = data;
            DataType = ObjectType.Bytes;
        }

        [Flags]
        public enum ObjectType { Bytes = 1, String = 2 }

        private const string DateFormat = "yyyy-MM-dd";

        public string Url { get; private set; }
        public DateTime Date { get; private set; }

        public ObjectType DataType { get; private set; }
        public byte[] ByteData { get; private set; }
        public string StringData { get; private set; }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Url);
            writer.Write(Date.ToString(DateFormat, CultureInfo.InvariantCulture));
            writer.Write((int)DataType);
            switch (DataType)
            {
                case ObjectType.Bytes:
                    writer.Write(ByteData.Length);
                    writer.Write(ByteData);
                    break;
                case ObjectType.String:
                    writer.Write(StringData);
                    break;
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            Url = reader.ReadString();
            Date = DateTime.ParseExact(reader.ReadString(), DateFormat, CultureInfo.InvariantCulture);
            DataType = (ObjectType) reader.ReadInt32();
            switch (DataType)
            {
                case ObjectType.Bytes:
                    ByteData = reader.ReadBytes(reader.ReadInt32());
                    break;
                case ObjectType.String:
                    StringData = reader.ReadString();
                    break;
            }
        }
    }
}