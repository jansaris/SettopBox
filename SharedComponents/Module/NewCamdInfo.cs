using System;

namespace SharedComponents.Module
{
    public class NewCamdInfo : ModuleInfo
    {
        public int NrOfClients { get; set; }
        public int NrOfChannels { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string ListeningAt { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DesKey { get; set; }
    }
}