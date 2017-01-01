using System;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using log4net;

namespace WebHelper
{
    public sealed class Compression
    {
        readonly ILog _logger;

        public Compression(ILog logger)
        {
            _logger = logger;
        }

        public byte[] Decompress(byte[] data)
        {
            try
            {
                if (data == null)
                {
                    throw new Exception("Null gzipFieName");
                }

                // Use a 4K buffer. Any larger is a waste.    
                var dataBuffer = new byte[4096];
                var outputStream = new MemoryStream();

                using (var gzipStream = new GZipInputStream(new MemoryStream(data)))
                {
                    StreamUtils.Copy(gzipStream, outputStream, dataBuffer);
                }
                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to extract GZip data", ex);
                return null;
            }
        }
    }
}
