using System.IO;
using log4net;

namespace EpgGrabber.IO
{
    public class FileDownloader : IFileDownloader
    {
        readonly ILog _logger;
        readonly IWebDownloader _webDownloader;

        public FileDownloader(IWebDownloader webDownloader, ILog logger)
        {
            _webDownloader = webDownloader;
            _logger = logger;
        }

        /// <summary>
        /// Downloads the binary file using the HTTP downloader
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="localFile">The local file to download to</param>
        public void DownloadBinaryFile(string url, string localFile)
        {
            EnsureDirectoryExist(localFile);
            _logger.Debug($"Download {url} to {localFile}");
            var content = _webDownloader.DownloadBinary(url);
            if (content == null)
            {
                _logger.Warn($"No data received, can't save {localFile}");
                return;
            }
            var file = File.OpenWrite(localFile);
            file.Write(content, 0, content.Length);
            file.Close();
        }

        private void EnsureDirectoryExist(string localFile)
        {
            _logger.Debug($"Test folder for file {localFile}");
            var file = new FileInfo(localFile);
            var dir = file.Directory;
            if (dir == null || dir.Exists) return;
            _logger.Info($"Folder {dir.FullName} doesn't exist, create it");
            dir.Create();
        }
    }
}