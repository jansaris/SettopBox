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

interface IKeyblockInfo extends IModuleInfo {
    HasValidKeyblock: boolean;
    NextRetrieval: Date;
    LastRetrieval: Date;
    ValidFrom: Date;
    ValidTo: Date;
}

interface ILog {
    Timestamp: Date;
    Module: string;
    Component: string;
    Message: string;
    Level: string;
}

interface ISetting {
    Name: string;
    Value: any;
    Type: string;
    InputType: string;
}