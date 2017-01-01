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

        public byte[] DownloadBinary(string url)
        {
            return _webDownloader.DownloadBinary(url);
        }

        public string DownloadString(string url)
        {
            return _webDownloader.DownloadString(url);
        }

        public void DownloadBinaryFile(string url, string localFile)
        {
            _fileDownloader.DownloadBinaryFile(url,localFile);
        }
    }
}