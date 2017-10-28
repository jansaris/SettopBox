namespace WebHelper
{
    public class Downloader : IDownloader
    {
        private readonly IWebDownloader _webDownloader;
        private readonly IFileDownloader _fileDownloader;

        public Downloader(IWebDownloader webDownloader, IFileDownloader fileDownloader)
        {
            _webDownloader = webDownloader;
            _fileDownloader = fileDownloader;
        }

        public byte[] DownloadBinary(string url, bool noCache = false)
        {
            return _webDownloader.DownloadBinary(url, noCache);
        }

        public string DownloadString(string url, bool noCache = false)
        {
            return _webDownloader.DownloadString(url, noCache);
        }

        public void DownloadBinaryFile(string url, string localFile)
        {
            _fileDownloader.DownloadBinaryFile(url,localFile);
        }
    }
}