using System;

namespace SharedComponents
{
    public enum ModuleState
    {
        Initial,
        Disabled,
        Starting,
        Running,
        Stopping,
        Stopped,
        Error
    };

    public interface IModule : IDisposable
    {
        string Name { get; }
        ModuleState State { get; }
        void Start();
        void Stop();
        void Disable();

        event EventHandler<ModuleState> StatusChanged;
    }
}