using System;

namespace EpgGrabber
{
    public interface IGrabber
    {
        string Download(Func<bool> stopProcessing);
    }
}