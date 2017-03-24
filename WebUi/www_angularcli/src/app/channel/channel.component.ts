import { Component, OnInit, Input  } from '@angular/core';
import { Channel, ChannelLocations, IptvInfo } from '../models';
import { SettopboxService } from '../settopbox.service';

@Component({
    selector: 'channel',
    templateUrl: './channel.component.html',
    styleUrls: ['./channel.component.css']
})
export class ChannelComponent implements OnInit {
    @Input()
    info: Channel;
    orgInfo: Channel;
    channels: IptvInfo[];

    changes: number;
    loading: boolean;
    TvHeadendLoading: boolean;
    noChannel: string;
    isCollapsed: boolean;

    constructor(private settopboxService: SettopboxService) {
        this.noChannel = null;
        this.channels = [];
    }

    ngOnInit() {
        this.updateOriginals(this.info);
    }

    updateOriginals(channel: Channel) {
        this.orgInfo = (JSON.parse(JSON.stringify(channel))) as Channel;
    }

    getNrOfChangedSettings(): number {
        var count = 0;
        if (this.orgInfo.EpgGrabber != this.info.EpgGrabber) count = count + 1;
        if (this.orgInfo.TvHeadend != this.info.TvHeadend) count = count + 1;
        if (this.orgInfo.Keyblock != this.info.Keyblock) count = count + 1;
        if (this.orgInfo.TvHeadendChannel != this.info.TvHeadendChannel) count = count + 1;
        return count;
    }

    onToggleButtonChange(name: string, enabled: boolean) {
        switch (name) {
            case 'EPG': this.info.EpgGrabber = enabled; break;
            case 'Keyblock': this.info.Keyblock = enabled; break;
        }
    }

    toggleCollapsed() {
        this.isCollapsed = !this.isCollapsed;
        this.loadChannels();
    }

    loadChannels() {
        if (this.channels.length > 0 || this.TvHeadendLoading) return;
        this.refreshChannels();
    }

    refreshChannels() {
        this.TvHeadendLoading = true;
        this.settopboxService
            .iptvInfo(this.info.Id)
            .then(r => {
                this.channels = r;
                this.TvHeadendLoading = false;
            }).catch(r => {
                this.TvHeadendLoading = false;
            });
    }

    saveSettings() {
        this.loading = true;
        this.settopboxService
            .set(this.info)
            .then(r => {
                this.updateOriginals(this.info);
                this.loading = false;
            }).catch(r => {
                this.loading = false;
            });
    }

    channelSelected() {
        var info = this.getIptvInfo(this.info.TvHeadendChannel);
        if (info) {
            this.info.Keyblock = true;
            this.info.KeyblockId = info.Number;
            this.info.TvHeadendChannel = info.Url;
            this.info.TvHeadend = true;
        }
        else {
            this.info.Keyblock = false;
            this.info.KeyblockId = null;
            this.info.TvHeadendChannel = null;
            this.info.TvHeadend = false;
        }
    }

    getIptvInfo(url: string) : IptvInfo {
        for (let info of this.channels) {
            if (info.Url == url) return info;
        }
        return null;
    }

    resetSettings() {
        this.info.EpgGrabber = this.orgInfo.EpgGrabber;
        this.info.TvHeadend = this.orgInfo.TvHeadend;
        this.info.Keyblock = this.orgInfo.Keyblock;
        this.info.TvHeadendChannel = this.orgInfo.TvHeadendChannel;
    }
}
