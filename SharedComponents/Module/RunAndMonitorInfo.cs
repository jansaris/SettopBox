namespace SharedComponents.Module
{
    public class RunAndMonitorInfo : IModuleInfo
    {
        public string ProcessName { get; set; }
        public string PID { get; set; }
        public string Status { get; set; }
    }
}