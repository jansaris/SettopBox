interface IModule {
    Name: string;
    Enabled: boolean;
    Status: string;
    Info: IModuleInfo;
}
interface IModuleInfo {

}

interface INewcamdInfo extends IModuleInfo {
    NrOfClients: number;
    NrOfChannels: number;
    ValidFrom: Date;
    ValidTo: Date;
    ListeningAt: string;
    Username: string;
    Password: string;
    DesKey: string;
}