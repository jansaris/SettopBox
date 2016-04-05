using SharedComponents.Module;

namespace WebUi.api.Models
{
    public class Module
    {
        public string Name { get; set; } 
        public bool Enabled { get; set; } 
        public string Status { get; set; }
        public IModuleInfo Info { get; set; }
    }
}