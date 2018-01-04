using System;

namespace EpgGrabber
{
    public class EpgGrabberException : Exception
    {
        public EpgGrabberException(string message) : base(message)
        {
        }

        public EpgGrabberException(string message, Exception baseException) : base(message, baseException)
        {
            
        }
    }
}