using System;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using log4net;

namespace EpgGrabber.IO
{
    public sealed class Compression
    {
        readonly ILog _logger;

        public Compression(ILog logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Decompresses the specified compressed file using SharpZipLib
        /// MkBundle doesn't support GZipStream
        /// </summary>
        /// <param name="gzipFileName">The compressed file.</param>
        /// <param name="filename">The uncompressed file.</param>
        public bool Decompress(string gzipFileName, string filename)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gzipFileName))
                {
                    throw new Exception("Null gzipFieName");
                }

                // Use a 4K buffer. Any larger is a waste.    
                var dataBuffer = new byte[4096];

                using (Stream fs = new FileStream(gzipFileName, FileMode.Open, FileAccess.Read))
                using (var gzipStream = new GZipInputStream(fs))
                {
                    // Change this to your needs
                    //var fnOut = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(gzipFileName));
                    using (var fsOut = File.Create(filename))
                    {
                        StreamUtils.Copy(gzipStream, fsOut, dataBuffer);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to extract {gzipFileName}", ex);
                return false;
            }
        }
    }
}
