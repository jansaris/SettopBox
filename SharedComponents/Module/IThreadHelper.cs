using System;
using System.Threading;
using log4net;

namespace SharedComponents.Module
{
    public interface IThreadHelper
    {
        Thread RunSafeInNewThread(Action action, ILog logger, ThreadPriority priority = ThreadPriority.Normal);
        void AbortThread(Thread thread, ILog logger, int msBeforeRealAbort = 1000);
    }
}