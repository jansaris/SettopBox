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
        IModuleInfo GetData();
        void ProcessDataFromOtherModule(string moduleName, CommunicationData data);

        event EventHandler<ModuleState> StatusChanged;
        event EventHandler<CommunicationData> NewDataAvailable;
    }
}