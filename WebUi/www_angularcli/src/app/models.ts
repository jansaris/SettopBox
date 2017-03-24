export class Module {
    Name: string;
    Enabled: boolean;
    Status: string;
    Info: ModuleInfo;
}

export class ModuleInfo {
}

export class NewcamdInfo extends ModuleInfo {
    NrOfClients: number;
    NrOfChannels: number;
    ValidFrom: Date;
    ValidTo: Date;
    ListeningAt: string;
    Username: string;
    Password: string;
    DesKey: string;
}

export class KeyblockInfo extends ModuleInfo {
    HasValidKeyblock: boolean;
    NextRetrieval: Date;
    LastRetrieval: Date;
    ValidFrom: Date;
    ValidTo: Date;
    RefreshAfter: Date;
}

export class EpgInfo extends ModuleInfo {
    LastRetrieval: Date;
    NextRetrieval: Date;
    Channels: string[];
}

export class TvheadendInfo extends ModuleInfo {
    LastEpgUpdate: Date;
    LastEpgUpdateSuccessfull: boolean;
}

export class ChannelListInfo extends ModuleInfo {
    LastRetrieval: Date;
    State: string;
    Channels: ChannelInfo[];
}

export class Log {
    Timestamp: Date;
    Module: string;
    Component: string;
    Message: string;
    Level: string;
}

export class Setting {
    Name: string;
    Value: any;
    ServerValue: any;
    Type: string;
    InputType: string;
}

export class Performance{
    Total: number;
    Process: number;
    Cores: number;
    Mono: number;
}

export class ChannelInfo {
    Key: string;
    Name: string;
    Locations: ChannelLocations[];
    Icons: string[];
    Radio: boolean;
    Number: number;
    FirstLocationUrl: string;
    FirstLocationQuality: string;
    DetailsVisible: boolean;
    DetailsTimer: any;
}

export class ChannelLocations {
    Name: string;
    Url: string;
}

export class Channel {
    Number: number;
    Id: string;
    Name: string;
    AvailableChannels: ChannelLocations[];
    TvHeadendChannel: string;
    KeyblockId: number;
    Keyblock: boolean;
    EpgGrabber: boolean;
    TvHeadend: boolean;
}

export class IptvInfo {
    Url: string;
    Name: string;
    Provider: string;
    Number: number;
    KBps: number;
    MBps: number;
}