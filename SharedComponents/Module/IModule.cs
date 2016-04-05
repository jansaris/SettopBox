using System;

namespace SharedComponents.Module
{
    public interface IModule : IDisposable
    {
        string Name { get; }
        ModuleState State { get; }
        void Start();
        void Stop();
        void Disable();

        IModuleInfo GetModuleInfo();

        event EventHandler<ModuleState> StatusChanged;
    }
}