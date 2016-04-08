using System;

namespace WebUi.api.Models
{
    public class LogModel
    {
        public DateTime Timestamp { get; set; }
        public string Module { get; set; }
        public string Component { get; set; }
        public string Message { get; set; }
        public string Level { get; set; }
    }
}