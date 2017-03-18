using System.Runtime.Serialization;
using SharedComponents.Module;
using SharedComponents.Models;

namespace WebUi.api.Models
{
    [KnownType(typeof(ChannelListInfo))]
    public class Module
    {
        public string Name { get; set; } 
        public bool Enabled { get; set; } 
        public string Status { get; set; }
        public IModuleInfo Info { get; set; }
    }
}