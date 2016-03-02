namespace Keyblock
{
    public interface IKeyblock
    {
        bool DownloadNew();
        void CleanUp();
    }
}