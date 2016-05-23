namespace EpgGrabber.IO
{
    public interface IFileDownloader
    {
        /// <summary>
        /// Downloads the binary file using the HTTP downloader
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="localFile">The local file to download to</param>
        void DownloadBinaryFile(string url, string localFile);
    }
}