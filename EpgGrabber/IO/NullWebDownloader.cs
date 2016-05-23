namespace EpgGrabber.IO
{
    public class NullWebDownloader : IWebDownloader
    {
        public byte[] DownloadBinary(string url)
        {
            return null;
        }

        public string DownloadString(string url)
        {
            return null;
        }
    }
}