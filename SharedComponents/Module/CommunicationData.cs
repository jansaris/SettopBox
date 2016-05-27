namespace SharedComponents.Module
{
    public class CommunicationData
    {
        public CommunicationData(DataType type, object data)
        {
            Type = type;
            Data = data;
        }

        public DataType Type { get; }
        public object Data { get; }
    }
}