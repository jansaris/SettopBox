namespace WebHelper
{
    public interface IWebDownloader
    {
        byte[] DownloadBinary(string url, bool noCache = false);
        string DownloadString(string url, bool noCache = false);
    }
}