using System;

namespace SharedComponents
{
    public interface IModule : IDisposable
    {
        string Name { get; }
        void Start();
        void Stop();
        void Disable();
    }
}