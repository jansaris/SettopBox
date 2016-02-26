using System;

namespace Keyblock
{
    public class KeyblockException : Exception
    {
        public KeyblockException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}