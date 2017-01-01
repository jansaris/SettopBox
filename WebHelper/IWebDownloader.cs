namespace WebHelper
{
    public interface IWebDownloader
    {
        byte[] DownloadBinary(string url);
        string DownloadString(string url);
    }
}